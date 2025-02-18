using Orleans.CodeGenerator.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using Orleans.CodeGenerator.Diagnostics;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Orleans.CodeGenerator
{
    /// <summary>
    /// Generates RPC stub objects called invokers.
    /// </summary>
    internal static class InvokableGenerator
    {
        public static (ClassDeclarationSyntax, GeneratedInvokerDescription) Generate(
            LibraryTypes libraryTypes,
            InvokableInterfaceDescription interfaceDescription,
            MethodDescription method)
        {
            var generatedClassName = GetSimpleClassName(interfaceDescription, method);
            INamedTypeSymbol baseClassType = GetBaseClassType(method);
            var fieldDescriptions = GetFieldDescriptions(method, interfaceDescription);
            var fields = GetFieldDeclarations(method, fieldDescriptions, libraryTypes);
            var (ctor, ctorArgs) = GenerateConstructor(libraryTypes, generatedClassName, method, fieldDescriptions, baseClassType);

            Accessibility accessibility = GetAccessibility(interfaceDescription);

            var targetField = fieldDescriptions.OfType<TargetFieldDescription>().Single();

            var accessibilityKind = accessibility switch
            {
                Accessibility.Public => SyntaxKind.PublicKeyword,
                _ => SyntaxKind.InternalKeyword,
            };
            var classDeclaration = ClassDeclaration(generatedClassName)
                .AddBaseListTypes(SimpleBaseType(baseClassType.ToTypeSyntax(method.TypeParameterSubstitutions)))
                .AddModifiers(Token(accessibilityKind), Token(SyntaxKind.SealedKeyword))
                .AddAttributeLists(
                    AttributeList(SingletonSeparatedList(CodeGenerator.GetGeneratedCodeAttributeSyntax())))
                .AddMembers(fields)
                .AddMembers(ctor);

            classDeclaration = AddOptionalMembers(classDeclaration,
                    GenerateGetArgumentCount(libraryTypes, method),
                    GenerateGetMethodName(libraryTypes, method),
                    GenerateGetInterfaceName(libraryTypes, method),
                    GenerateGetActivityName(libraryTypes, method),
                    GenerateGetInterfaceType(libraryTypes, method),
                    GenerateGetMethod(libraryTypes),
                    GenerateSetTargetMethod(libraryTypes, interfaceDescription, targetField),
                    GenerateGetTargetMethod(libraryTypes, targetField),
                    GenerateDisposeMethod(libraryTypes, method, fieldDescriptions, baseClassType),
                    GenerateGetArgumentMethod(libraryTypes, method, fieldDescriptions),
                    GenerateSetArgumentMethod(libraryTypes, method, fieldDescriptions),
                    GenerateInvokeInnerMethod(libraryTypes, method, fieldDescriptions, targetField));

            var typeParametersWithNames = method.AllTypeParameters;
            if (typeParametersWithNames.Count > 0)
            {
                classDeclaration = SyntaxFactoryUtility.AddGenericTypeParameters(classDeclaration, typeParametersWithNames);
            }

            List<INamedTypeSymbol> serializationHooks = new();
            if (baseClassType.GetAttributes(libraryTypes.SerializationCallbacksAttribute, out var hookAttributes))
            {
                foreach (var hookAttribute in hookAttributes)
                {
                    var hookType = (INamedTypeSymbol)hookAttribute.ConstructorArguments[0].Value;
                    serializationHooks.Add(hookType);
                }
            }

            var invokerDescription = new GeneratedInvokerDescription(
                interfaceDescription,
                method,
                accessibility,
                generatedClassName,
                fieldDescriptions.OfType<IMemberDescription>().ToList(),
                serializationHooks,
                baseClassType,
                ctorArgs);
            return (classDeclaration, invokerDescription);

            static Accessibility GetAccessibility(InvokableInterfaceDescription interfaceDescription)
            {
                var t = interfaceDescription.InterfaceType;
                Accessibility accessibility = t.DeclaredAccessibility;
                while (t is not null)
                {
                    if ((int)t.DeclaredAccessibility < (int)accessibility)
                    {
                        accessibility = t.DeclaredAccessibility;
                    }

                    t = t.ContainingType;
                }

                return accessibility;
            }
        }

        private static ClassDeclarationSyntax AddOptionalMembers(ClassDeclarationSyntax decl, params MemberDeclarationSyntax[] items)
            => decl.WithMembers(decl.Members.AddRange(items.Where(i => i != null)));

        private static INamedTypeSymbol GetBaseClassType(MethodDescription method)
        {
            var methodReturnType = method.Method.ReturnType;
            if (methodReturnType is not INamedTypeSymbol namedMethodReturnType)
            {
                var diagnostic = InvalidGrainMethodReturnTypeDiagnostic.CreateDiagnostic(method);
                throw new OrleansGeneratorDiagnosticAnalysisException(diagnostic);
            }
            if (method.InvokableBaseTypes.TryGetValue(namedMethodReturnType, out var baseClassType))
            {
                return baseClassType;
            }

            if (namedMethodReturnType.ConstructedFrom is { } constructedFrom)
            {
                var unbound = constructedFrom.ConstructUnboundGenericType();
                if (method.InvokableBaseTypes.TryGetValue(unbound, out baseClassType))
                {
                    return baseClassType.ConstructedFrom.Construct(namedMethodReturnType.TypeArguments.ToArray());
                }
            }
            
            throw new InvalidOperationException($"Unsupported return type {methodReturnType} for method {method.Method.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
        }

        private static MemberDeclarationSyntax GenerateSetTargetMethod(
            LibraryTypes libraryTypes,
            InvokableInterfaceDescription interfaceDescription,
            TargetFieldDescription targetField)
        {
            var holder = IdentifierName("holder");
            var holderParameter = holder.Identifier;

            var getTarget = InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        holder,
                        GenericName(interfaceDescription.IsExtension ? "GetComponent" : "GetTarget")
                            .WithTypeArgumentList(
                                TypeArgumentList(
                                    SingletonSeparatedList(interfaceDescription.InterfaceType.ToTypeSyntax())))))
                .WithArgumentList(ArgumentList());

            var body =
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(targetField.FieldName),
                    getTarget);
            return MethodDeclaration(libraryTypes.Void.ToTypeSyntax(), "SetTarget")
                .WithParameterList(ParameterList(SingletonSeparatedList(Parameter(holderParameter).WithType(libraryTypes.ITargetHolder.ToTypeSyntax()))))
                .WithExpressionBody(ArrowExpressionClause(body))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)));
        }

        private static MemberDeclarationSyntax GenerateGetTargetMethod(LibraryTypes libraryTypes, TargetFieldDescription targetField)
        {
            return MethodDeclaration(libraryTypes.Object.ToTypeSyntax(), "GetTarget")
                .WithParameterList(ParameterList())
                .WithExpressionBody(ArrowExpressionClause(IdentifierName(targetField.FieldName)))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)));
        }

        private static MemberDeclarationSyntax GenerateGetArgumentMethod(
            LibraryTypes libraryTypes,
            MethodDescription methodDescription,
            List<InvokerFieldDescripton> fields)
        {
            if (methodDescription.Method.Parameters.Length == 0)
                return null;

            var index = IdentifierName("index");

            var cases = new List<SwitchSectionSyntax>();
            foreach (var field in fields)
            {
                if (field is not MethodParameterFieldDescription parameter)
                {
                    continue;
                }

                // C#: case {index}: return {field}
                var label = CaseSwitchLabel(
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(parameter.ParameterOrdinal)));
                cases.Add(
                    SwitchSection(
                        SingletonList<SwitchLabelSyntax>(label),
                        new SyntaxList<StatementSyntax>(
                            ReturnStatement(
                                IdentifierName(parameter.FieldName)))));
            }

            // C#: default: return OrleansGeneratedCodeHelper.InvokableThrowArgumentOutOfRange(index, {maxArgs})
            var throwHelperMethod = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("OrleansGeneratedCodeHelper"),
                IdentifierName("InvokableThrowArgumentOutOfRange"));
            cases.Add(
                SwitchSection(
                    SingletonList<SwitchLabelSyntax>(DefaultSwitchLabel()),
                    new SyntaxList<StatementSyntax>(
                        ReturnStatement(
                            InvocationExpression(
                                throwHelperMethod,
                                ArgumentList(
                                    SeparatedList(
                                        new[]
                                        {
                                            Argument(index),
                                            Argument(
                                                LiteralExpression(
                                                    SyntaxKind.NumericLiteralExpression,
                                                    Literal(
                                                        Math.Max(0, methodDescription.Method.Parameters.Length - 1))))
                                        })))))));
            var body = SwitchStatement(index, new SyntaxList<SwitchSectionSyntax>(cases));
            return MethodDeclaration(libraryTypes.Object.ToTypeSyntax(), "GetArgument")
                .WithParameterList(
                    ParameterList(
                        SingletonSeparatedList(
                            Parameter(Identifier("index")).WithType(libraryTypes.Int32.ToTypeSyntax()))))
                .WithBody(Block(body))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)));
        }

        private static MemberDeclarationSyntax GenerateSetArgumentMethod(
            LibraryTypes libraryTypes,
            MethodDescription methodDescription,
            List<InvokerFieldDescripton> fields)
        {
            if (methodDescription.Method.Parameters.Length == 0)
                return null;

            var index = IdentifierName("index");
            var value = IdentifierName("value");

            var cases = new List<SwitchSectionSyntax>();
            foreach (var field in fields)
            {
                if (field is not MethodParameterFieldDescription parameter)
                {
                    continue;
                }

                // C#: case {index}: {field} = (TField)value; return;
                var label = CaseSwitchLabel(
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(parameter.ParameterOrdinal)));
                cases.Add(
                    SwitchSection(
                        SingletonList<SwitchLabelSyntax>(label),
                        new SyntaxList<StatementSyntax>(
                            new StatementSyntax[]
                            {
                                ExpressionStatement(
                                    AssignmentExpression(
                                        SyntaxKind.SimpleAssignmentExpression,
                                        IdentifierName(parameter.FieldName),
                                        CastExpression(parameter.FieldType.ToTypeSyntax(methodDescription.TypeParameterSubstitutions), value))),
                                ReturnStatement()
                            })));
            }

            // C#: default: OrleansGeneratedCodeHelper.InvokableThrowArgumentOutOfRange(index, {maxArgs})
            var maxArgs = Math.Max(0, methodDescription.Method.Parameters.Length - 1);
            var throwHelperMethod = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("OrleansGeneratedCodeHelper"),
                IdentifierName("InvokableThrowArgumentOutOfRange"));
            cases.Add(
                SwitchSection(
                    SingletonList<SwitchLabelSyntax>(DefaultSwitchLabel()),
                    new SyntaxList<StatementSyntax>(
                        new StatementSyntax[]
                        {
                            ExpressionStatement(
                                InvocationExpression(
                                    throwHelperMethod,
                                    ArgumentList(
                                        SeparatedList(
                                            new[]
                                            {
                                                Argument(index),
                                                Argument(
                                                    LiteralExpression(
                                                        SyntaxKind.NumericLiteralExpression,
                                                        Literal(maxArgs)))
                                            })))),
                            ReturnStatement()
                        })));
            var body = SwitchStatement(index, new SyntaxList<SwitchSectionSyntax>(cases));
            return MethodDeclaration(libraryTypes.Void.ToTypeSyntax(), "SetArgument")
                .WithParameterList(
                    ParameterList(
                        SeparatedList(
                            new[]
                            {
                                Parameter(Identifier("index")).WithType(libraryTypes.Int32.ToTypeSyntax()),
                                Parameter(Identifier("value")).WithType(libraryTypes.Object.ToTypeSyntax())
                            }
                        )))
                .WithBody(Block(body))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)));
        }

        private static MemberDeclarationSyntax GenerateInvokeInnerMethod(
            LibraryTypes libraryTypes,
            MethodDescription method,
            List<InvokerFieldDescripton> fields,
            TargetFieldDescription target)
        {
            var resultTask = IdentifierName("resultTask");

            // C# var resultTask = this.target.{Method}({params});
            var args = SeparatedList(
                fields.OfType<MethodParameterFieldDescription>()
                    .OrderBy(p => p.ParameterOrdinal)
                    .Select(p => Argument(ThisExpression().Member(p.FieldName))));
            ExpressionSyntax methodCall;
            if (method.MethodTypeParameters.Count > 0)
            {
                methodCall = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ThisExpression().Member(target.FieldName),
                    GenericName(
                        Identifier(method.Method.Name),
                        TypeArgumentList(
                            SeparatedList<TypeSyntax>(
                                method.MethodTypeParameters.Select(p => IdentifierName(p.Name))))));
            }
            else
            {
                methodCall = ThisExpression().Member(target.FieldName).Member(method.Method.Name);
            }

            return MethodDeclaration(method.Method.ReturnType.ToTypeSyntax(method.TypeParameterSubstitutions), "InvokeInner")
                .WithParameterList(ParameterList())
                .WithExpressionBody(ArrowExpressionClause(InvocationExpression(methodCall, ArgumentList(args))))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                .WithModifiers(TokenList(Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.OverrideKeyword)));
        }

        private static MemberDeclarationSyntax GenerateDisposeMethod(
            LibraryTypes libraryTypes,
            MethodDescription methodDescription,
            List<InvokerFieldDescripton> fields,
            INamedTypeSymbol baseClassType)
        {
            var body = new List<StatementSyntax>();
            foreach (var field in fields)
            {
                if (field.IsInstanceField)
                {
                    body.Add(
                        ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                ThisExpression().Member(field.FieldName),
                                DefaultExpression(field.FieldType.ToTypeSyntax(methodDescription.TypeParameterSubstitutions)))));
                }
            }

            // C# base.Dispose();
            if (baseClassType is { }
                && baseClassType.AllInterfaces.Contains(libraryTypes.IDisposable)
                && baseClassType.GetAllMembers<IMethodSymbol>("Dispose").FirstOrDefault(m => !m.IsAbstract && m.DeclaredAccessibility != Accessibility.Private) is { })
            {
                body.Add(ExpressionStatement(InvocationExpression(BaseExpression().Member("Dispose")).WithArgumentList(ArgumentList())));
            }

            return MethodDeclaration(libraryTypes.Void.ToTypeSyntax(), "Dispose")
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)))
                .WithBody(Block(body));
        }

        private static MemberDeclarationSyntax GenerateGetArgumentCount(LibraryTypes libraryTypes, MethodDescription methodDescription)
            => methodDescription.Method.Parameters.Length is var count and not 0 ?
            MethodDeclaration(libraryTypes.Int32.ToTypeSyntax(), "GetArgumentCount")
                .WithExpressionBody(ArrowExpressionClause(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(count))))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)) : null;

        private static MemberDeclarationSyntax GenerateGetActivityName(
            LibraryTypes libraryTypes,
            MethodDescription methodDescription)
        {
            // This property is intended to contain a value suitable for use as an OpenTelemetry Span Name for RPC calls.
            // Therefore, the interface name and method name components must not include periods or slashes.
            // In order to avoid that, we omit the namespace from the interface name.
            // See: https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/rpc.md

            var interfaceName = methodDescription.Method.ContainingType.ToDisplayName(methodDescription.TypeParameterSubstitutions, includeGlobalSpecifier: false, includeNamespace: false);
            var methodName = methodDescription.Method.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var activityName = $"{interfaceName}/{methodName}";
            return MethodDeclaration(PredefinedType(Token(SyntaxKind.StringKeyword)), "GetActivityName")
                .WithExpressionBody(
                    ArrowExpressionClause(
                        LiteralExpression(
                            SyntaxKind.NumericLiteralExpression,
                            Literal(activityName))))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
        }

        private static MemberDeclarationSyntax GenerateGetMethodName(
            LibraryTypes libraryTypes,
            MethodDescription methodDescription) =>
            MethodDeclaration(PredefinedType(Token(SyntaxKind.StringKeyword)), "GetMethodName")
                .WithExpressionBody(
                    ArrowExpressionClause(
                        LiteralExpression(
                            SyntaxKind.NumericLiteralExpression,
                            Literal(methodDescription.Method.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

        private static MemberDeclarationSyntax GenerateGetInterfaceName(
            LibraryTypes libraryTypes,
            MethodDescription methodDescription) =>
            MethodDeclaration(PredefinedType(Token(SyntaxKind.StringKeyword)), "GetInterfaceName")
                .WithExpressionBody(
                    ArrowExpressionClause(
                        LiteralExpression(
                            SyntaxKind.NumericLiteralExpression,
                            Literal(methodDescription.Method.ContainingType.ToDisplayName(methodDescription.TypeParameterSubstitutions, includeGlobalSpecifier: false)))))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

        private static MemberDeclarationSyntax GenerateGetInterfaceType(
            LibraryTypes libraryTypes,
            MethodDescription methodDescription) =>
            MethodDeclaration(libraryTypes.Type.ToTypeSyntax(), "GetInterfaceType")
                .WithExpressionBody(
                    ArrowExpressionClause(
                        TypeOfExpression(methodDescription.Method.ContainingType.ToTypeSyntax(methodDescription.TypeParameterSubstitutions))))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

        private static MemberDeclarationSyntax GenerateGetMethod(
            LibraryTypes libraryTypes)
            => MethodDeclaration(libraryTypes.MethodInfo.ToTypeSyntax(), "GetMethod")
                .WithExpressionBody(ArrowExpressionClause(IdentifierName("MethodBackingField")))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

        public static string GetSimpleClassName(InvokableInterfaceDescription interfaceDescription, MethodDescription method)
        {
            var genericArity = method.AllTypeParameters.Count;
            var typeArgs = genericArity > 0 ? "_" + genericArity : string.Empty;
            return $"Invokable_{interfaceDescription.Name}_{interfaceDescription.ProxyBaseType.Name}_{method.Name}{typeArgs}";
        }

        private static MemberDeclarationSyntax[] GetFieldDeclarations(
            MethodDescription method,
            List<InvokerFieldDescripton> fieldDescriptions,
            LibraryTypes libraryTypes)
        {
            return fieldDescriptions.Select(GetFieldDeclaration).ToArray();

            MemberDeclarationSyntax GetFieldDeclaration(InvokerFieldDescripton description)
            {
                FieldDeclarationSyntax field;
                if (description is MethodInfoFieldDescription methodInfo)
                {
                    var methodTypeArguments = GetTypesArray(method, method.MethodTypeParameters.Select(p => p.Parameter));
                    var parameterTypes = GetTypesArray(method, method.Method.Parameters.Select(p => p.Type));

                    field = FieldDeclaration(
                        VariableDeclaration(
                            libraryTypes.MethodInfo.ToTypeSyntax(),
                            SingletonSeparatedList(VariableDeclarator(description.FieldName)
                            .WithInitializer(EqualsValueClause(
                                InvocationExpression(
                                    IdentifierName("OrleansGeneratedCodeHelper").Member("GetMethodInfoOrDefault"),
                                    ArgumentList(SeparatedList(new[]
                                    {
                                        Argument(TypeOfExpression(method.Method.ContainingType.ToTypeSyntax(method.TypeParameterSubstitutions))),
                                        Argument(method.Method.Name.GetLiteralExpression()),
                                        Argument(methodTypeArguments),
                                        Argument(parameterTypes),
                                    }))))))))
                        .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ReadOnlyKeyword));
                }
                else
                {
                    field = FieldDeclaration(
                        VariableDeclaration(
                            description.FieldType.ToTypeSyntax(method.TypeParameterSubstitutions),
                            SingletonSeparatedList(VariableDeclarator(description.FieldName))));
                }

                switch (description)
                {
                    case MethodParameterFieldDescription _:
                        field = field.AddModifiers(Token(SyntaxKind.PublicKeyword));
                        break;
                }

                if (!description.IsSerializable)
                {
                    if (description.IsInstanceField)
                        field = field.AddAttributeLists(
                                AttributeList()
                                    .AddAttributes(Attribute(libraryTypes.NonSerializedAttribute.ToNameSyntax())));
                }
                else if (description is MethodParameterFieldDescription parameter)
                {
                    field = field.AddAttributeLists(
                        AttributeList()
                            .AddAttributes(
                                Attribute(
                                    libraryTypes.IdAttributeTypes[0].ToNameSyntax(),
                                    AttributeArgumentList()
                                        .AddArguments(
                                            AttributeArgument(
                                                LiteralExpression(
                                                    SyntaxKind.NumericLiteralExpression,
                                                    Literal(parameter.FieldId)))))));
                }

                return field;
            }
        }

        private static ExpressionSyntax GetTypesArray(MethodDescription method, IEnumerable<ITypeSymbol> typeSymbols)
        {
            var types = typeSymbols.ToArray();
            return types.Length == 0 ? LiteralExpression(SyntaxKind.NullLiteralExpression)
                : ImplicitArrayCreationExpression(InitializerExpression(SyntaxKind.ArrayInitializerExpression, SeparatedList<ExpressionSyntax>(
                    types.Select(t => TypeOfExpression(t.ToTypeSyntax(method.TypeParameterSubstitutions))))));
        }

        private static (ConstructorDeclarationSyntax Constructor, List<TypeSyntax> ConstructorArguments) GenerateConstructor(
            LibraryTypes libraryTypes,
            string simpleClassName,
            MethodDescription method,
            List<InvokerFieldDescripton> fieldDescriptions,
            INamedTypeSymbol baseClassType)
        {
            var parameters = new List<ParameterSyntax>();

            var body = new List<StatementSyntax>();

            List<TypeSyntax> constructorArgumentTypes = new();
            List<ArgumentSyntax> baseConstructorArguments = new();
            foreach (var constructor in baseClassType.GetAllMembers<IMethodSymbol>())
            {
                if (constructor.MethodKind != MethodKind.Constructor || constructor.DeclaredAccessibility == Accessibility.Private || constructor.IsImplicitlyDeclared)
                {
                    continue;
                }

                if (constructor.HasAttribute(libraryTypes.GeneratedActivatorConstructorAttribute))
                {
                    var index = 0;
                    foreach (var parameter in constructor.Parameters)
                    {
                        var identifier = $"base{index}";

                        var argumentType = parameter.Type.ToTypeSyntax(method.TypeParameterSubstitutions);
                        constructorArgumentTypes.Add(argumentType);
                        parameters.Add(Parameter(identifier.ToIdentifier()).WithType(argumentType));
                        baseConstructorArguments.Add(Argument(identifier.ToIdentifierName()));
                        index++;
                    }
                    break;
                }
            }

            foreach (var (methodName, methodArgument) in method.CustomInitializerMethods)
            {
                var argumentExpression = methodArgument.ToExpression();
                body.Add(ExpressionStatement(InvocationExpression(ThisExpression().Member(methodName), ArgumentList(SeparatedList(new[] { Argument(argumentExpression) })))));
            }

            var constructorDeclaration = ConstructorDeclaration(simpleClassName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(parameters.ToArray())
                .WithInitializer(
                    ConstructorInitializer(
                        SyntaxKind.BaseConstructorInitializer,
                        ArgumentList(SeparatedList(baseConstructorArguments))))
                .AddAttributeLists(AttributeList(SingletonSeparatedList(Attribute(libraryTypes.GeneratedActivatorConstructorAttribute.ToNameSyntax()))))
                .AddBodyStatements(body.ToArray());

            return (constructorDeclaration, constructorArgumentTypes);
        }

        private static List<InvokerFieldDescripton> GetFieldDescriptions(
            MethodDescription method,
            InvokableInterfaceDescription interfaceDescription)
        {
            var fields = new List<InvokerFieldDescripton>();

            ushort fieldId = 0;
            foreach (var parameter in method.Method.Parameters)
            {
                fields.Add(new MethodParameterFieldDescription(method, parameter, $"arg{fieldId}", fieldId));
                fieldId++;
            }

            fields.Add(new TargetFieldDescription(interfaceDescription.InterfaceType));
            fields.Add(new MethodInfoFieldDescription(interfaceDescription.CodeGenerator.LibraryTypes.MethodInfo, "MethodBackingField"));

            return fields;
        }

        internal abstract class InvokerFieldDescripton
        {
            protected InvokerFieldDescripton(ITypeSymbol fieldType, string fieldName)
            {
                FieldType = fieldType;
                FieldName = fieldName;
            }

            public ITypeSymbol FieldType { get; }
            public string FieldName { get; }
            public abstract bool IsSerializable { get; }
            public abstract bool IsInstanceField { get; }
        }

        internal sealed class TargetFieldDescription : InvokerFieldDescripton
        {
            public TargetFieldDescription(ITypeSymbol fieldType) : base(fieldType, "target") { }

            public override bool IsSerializable => false;
            public override bool IsInstanceField => true;
        }

        internal sealed class MethodParameterFieldDescription : InvokerFieldDescripton, IMemberDescription
        {
            public MethodParameterFieldDescription(MethodDescription method, IParameterSymbol parameter, string fieldName, ushort fieldId)
                : base(parameter.Type, fieldName)
            {
                Method = method;
                FieldId = fieldId;
                Parameter = parameter;
                if (parameter.Type.TypeKind == TypeKind.Dynamic)
                {
                    TypeSyntax = PredefinedType(Token(SyntaxKind.ObjectKeyword));
                    TypeName = "dynamic";
                }
                else
                {
                    TypeName = Type.ToDisplayName(method.TypeParameterSubstitutions);
                    TypeSyntax = Type.ToTypeSyntax(method.TypeParameterSubstitutions);
                }

                Symbol = parameter;
            }

            public ISymbol Symbol { get; }
            public MethodDescription Method { get; }
            public int ParameterOrdinal => Parameter.Ordinal;
            public ushort FieldId { get; }
            public ISymbol Member => Parameter;
            public ITypeSymbol Type => FieldType;
            public INamedTypeSymbol ContainingType => Parameter.ContainingType;
            public TypeSyntax TypeSyntax { get; }
            public IParameterSymbol Parameter { get; }
            public override bool IsSerializable => true;
            public override bool IsInstanceField => true;

            public string AssemblyName => Parameter.Type.ContainingAssembly.ToDisplayName();
            public string TypeName { get; }

            public string TypeNameIdentifier
            {
                get
                {
                    if (Type is ITypeParameterSymbol tp && Method.TypeParameterSubstitutions.TryGetValue(tp, out var name))
                    {
                        return name;
                    }

                    return Type.GetValidIdentifier();
                }
            }

            public bool IsPrimaryConstructorParameter => false;

            public TypeSyntax GetTypeSyntax(ITypeSymbol typeSymbol) => typeSymbol.ToTypeSyntax(Method.TypeParameterSubstitutions);
        }

        internal sealed class MethodInfoFieldDescription : InvokerFieldDescripton
        {
            public MethodInfoFieldDescription(ITypeSymbol fieldType, string fieldName) : base(fieldType, fieldName) { }

            public override bool IsSerializable => false;
            public override bool IsInstanceField => false;
        }
    }
}