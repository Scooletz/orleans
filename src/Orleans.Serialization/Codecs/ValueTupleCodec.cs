using System;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.GeneratedCodeHelpers;
using Orleans.Serialization.WireProtocol;

namespace Orleans.Serialization.Codecs
{
    /// <summary>
    /// Serializer for <see cref="ValueTuple"/>.
    /// </summary>
    [RegisterSerializer]
    public sealed class ValueTupleCodec : IFieldCodec<ValueTuple>
    {
        /// <inheritdoc />
        void IFieldCodec<ValueTuple>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, ValueTuple value)
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.VarInt);
            writer.WriteVarUInt32(0);
        }

        /// <inheritdoc />
        ValueTuple IFieldCodec<ValueTuple>.ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType != WireType.VarInt)
            {
                ThrowUnsupportedWireTypeException();
            }

            ReferenceCodec.MarkValueField(reader.Session);
            _ = reader.ReadVarUInt64();

            return default;
        }

        internal static void ThrowUnsupportedWireTypeException() => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited} is supported for tuple fields.");
    }

    /// <summary>
    /// Copier for <see cref="ValueTuple"/>.
    /// </summary>
    [RegisterCopier]
    public class ValueTupleCopier : IDeepCopier<ValueTuple>
    {
        public ValueTuple DeepCopy(ValueTuple input, CopyContext _) => input;
    }

    /// <summary>
    /// Serializer for <see cref="ValueTuple{T1}"/>.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    [RegisterSerializer]
    public sealed class ValueTupleCodec<T> : IFieldCodec<ValueTuple<T>>
    {
        private static readonly Type ElementType1 = typeof(T);

        private readonly IFieldCodec<T> _valueCodec;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleCodec{T}"/> class.
        /// </summary>
        /// <param name="valueCodec">The value codec.</param>
        public ValueTupleCodec(IFieldCodec<T> valueCodec)
        {
            _valueCodec = OrleansGeneratedCodeHelper.UnwrapService(this, valueCodec);
        }

        /// <inheritdoc />
        void IFieldCodec<ValueTuple<T>>.WriteField<TBufferWriter>(
            ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            ValueTuple<T> value)
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            _valueCodec.WriteField(ref writer, 1, ElementType1, value.Item1);

            writer.WriteEndObject();
        }

        /// <inheritdoc />
        ValueTuple<T> IFieldCodec<ValueTuple<T>>.ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType != WireType.TagDelimited)
            {
                ValueTupleCodec.ThrowUnsupportedWireTypeException();
            }

            ReferenceCodec.MarkValueField(reader.Session);
            var item1 = default(T);
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader();
                if (header.IsEndBaseOrEndObject)
                {
                    break;
                }

                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 1:
                        item1 = _valueCodec.ReadValue(ref reader, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(header);
                        break;
                }
            }

            return new ValueTuple<T>(item1);
        }
    }

    /// <summary>
    /// Copier for <see cref="ValueTuple{T}"/>.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    [RegisterCopier]
    public class ValueTupleCopier<T> : IDeepCopier<ValueTuple<T>>
    {
        private readonly IDeepCopier<T> _copier;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleCopier{T}"/> class.
        /// </summary>
        /// <param name="copier">The copier.</param>
        public ValueTupleCopier(IDeepCopier<T> copier)
        {
            _copier = copier;
        }

        /// <inheritdoc />
        public ValueTuple<T> DeepCopy(ValueTuple<T> input, CopyContext context) => new(_copier.DeepCopy(input.Item1, context));
    }

    /// <summary>
    /// Serializer for <see cref="ValueTuple{T1, T2}"/>
    /// </summary>
    /// <typeparam name="T1">The type of the tuple's first component.</typeparam>
    /// <typeparam name="T2">The type of the tuple's second component.</typeparam>
    [RegisterSerializer]
    public sealed class ValueTupleCodec<T1, T2> : IFieldCodec<ValueTuple<T1, T2>>
    {
        private static readonly Type ElementType1 = typeof(T1);
        private static readonly Type ElementType2 = typeof(T2);

        private readonly IFieldCodec<T1> _item1Codec;
        private readonly IFieldCodec<T2> _item2Codec;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleCodec{T1, T2}"/> class.
        /// </summary>
        /// <param name="item1Codec">The <typeparamref name="T1"/> codec.</param>
        /// <param name="item2Codec">The <typeparamref name="T2"/> codec.</param>
        public ValueTupleCodec(IFieldCodec<T1> item1Codec, IFieldCodec<T2> item2Codec)
        {
            _item1Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item1Codec);
            _item2Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item2Codec);
        }

        /// <inheritdoc />
        void IFieldCodec<ValueTuple<T1, T2>>.WriteField<TBufferWriter>(
            ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            (T1, T2) value)
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            _item1Codec.WriteField(ref writer, 1, ElementType1, value.Item1);
            _item2Codec.WriteField(ref writer, 1, ElementType2, value.Item2);

            writer.WriteEndObject();
        }

        /// <inheritdoc />
        (T1, T2) IFieldCodec<ValueTuple<T1, T2>>.ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType != WireType.TagDelimited)
            {
                ValueTupleCodec.ThrowUnsupportedWireTypeException();
            }

            ReferenceCodec.MarkValueField(reader.Session);
            var item1 = default(T1);
            var item2 = default(T2);
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader();
                if (header.IsEndBaseOrEndObject)
                {
                    break;
                }

                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 1:
                        item1 = _item1Codec.ReadValue(ref reader, header);
                        break;
                    case 2:
                        item2 = _item2Codec.ReadValue(ref reader, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(header);
                        break;
                }
            }

            return new ValueTuple<T1, T2>(item1, item2);
        }
    }

    /// <summary>
    /// Copier for <see cref="ValueTuple{T1, T2}"/>
    /// </summary>
    /// <typeparam name="T1">The type of the tuple's first component.</typeparam>
    /// <typeparam name="T2">The type of the tuple's second component.</typeparam>
    [RegisterCopier]
    public class ValueTupleCopier<T1, T2> : IDeepCopier<ValueTuple<T1, T2>>
    {
        private readonly IDeepCopier<T1> _copier1;
        private readonly IDeepCopier<T2> _copier2;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleCopier{T1, T2}"/> class.
        /// </summary>
        /// <param name="copier1">The copier for <typeparamref name="T1"/>.</param>
        /// <param name="copier2">The copier for <typeparamref name="T2"/>.</param>
        public ValueTupleCopier(IDeepCopier<T1> copier1, IDeepCopier<T2> copier2)
        {
            _copier1 = copier1;
            _copier2 = copier2;
        }

        /// <inheritdoc />
        public ValueTuple<T1, T2> DeepCopy(ValueTuple<T1, T2> input, CopyContext context) => ValueTuple.Create(
                _copier1.DeepCopy(input.Item1, context),
                _copier2.DeepCopy(input.Item2, context));
    }

    /// <summary>
    /// Serializer for <see cref="ValueTuple{T1, T2, T3}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of the tuple's first component.</typeparam>
    /// <typeparam name="T2">The type of the tuple's second component.</typeparam>
    /// <typeparam name="T3">The type of the tuple's third component.</typeparam>
    [RegisterSerializer]
    public sealed class ValueTupleCodec<T1, T2, T3> : IFieldCodec<ValueTuple<T1, T2, T3>>
    {
        private static readonly Type ElementType1 = typeof(T1);
        private static readonly Type ElementType2 = typeof(T2);
        private static readonly Type ElementType3 = typeof(T3);

        private readonly IFieldCodec<T1> _item1Codec;
        private readonly IFieldCodec<T2> _item2Codec;
        private readonly IFieldCodec<T3> _item3Codec;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleCodec{T1, T2, T3}"/> class.
        /// </summary>
        /// <param name="item1Codec">The <typeparamref name="T1"/> codec.</param>
        /// <param name="item2Codec">The <typeparamref name="T2"/> codec.</param>
        /// <param name="item3Codec">The <typeparamref name="T3"/> codec.</param>
        public ValueTupleCodec(
            IFieldCodec<T1> item1Codec,
            IFieldCodec<T2> item2Codec,
            IFieldCodec<T3> item3Codec)
        {
            _item1Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item1Codec);
            _item2Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item2Codec);
            _item3Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item3Codec);
        }

        /// <inheritdoc />
        void IFieldCodec<ValueTuple<T1, T2, T3>>.WriteField<TBufferWriter>(
            ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            (T1, T2, T3) value)
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            _item1Codec.WriteField(ref writer, 1, ElementType1, value.Item1);
            _item2Codec.WriteField(ref writer, 1, ElementType2, value.Item2);
            _item3Codec.WriteField(ref writer, 1, ElementType3, value.Item3);

            writer.WriteEndObject();
        }

        /// <inheritdoc />
        (T1, T2, T3) IFieldCodec<ValueTuple<T1, T2, T3>>.ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType != WireType.TagDelimited)
            {
                ValueTupleCodec.ThrowUnsupportedWireTypeException();
            }

            ReferenceCodec.MarkValueField(reader.Session);
            var item1 = default(T1);
            var item2 = default(T2);
            var item3 = default(T3);
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader();
                if (header.IsEndBaseOrEndObject)
                {
                    break;
                }

                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 1:
                        item1 = _item1Codec.ReadValue(ref reader, header);
                        break;
                    case 2:
                        item2 = _item2Codec.ReadValue(ref reader, header);
                        break;
                    case 3:
                        item3 = _item3Codec.ReadValue(ref reader, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(header);
                        break;
                }
            }

            return new ValueTuple<T1, T2, T3>(item1, item2, item3);
        }
    }

    /// <summary>
    /// Copier for <see cref="ValueTuple{T1, T2, T3}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of the tuple's first component.</typeparam>
    /// <typeparam name="T2">The type of the tuple's second component.</typeparam>
    /// <typeparam name="T3">The type of the tuple's third component.</typeparam>
    [RegisterCopier]
    public class ValueTupleCopier<T1, T2, T3> : IDeepCopier<ValueTuple<T1, T2, T3>>
    {
        private readonly IDeepCopier<T1> _copier1;
        private readonly IDeepCopier<T2> _copier2;
        private readonly IDeepCopier<T3> _copier3;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleCopier{T1, T2, T3}"/> class.
        /// </summary>
        /// <param name="copier1">The <typeparamref name="T1"/> copier.</param>
        /// <param name="copier2">The <typeparamref name="T2"/> copier.</param>
        /// <param name="copier3">The <typeparamref name="T3"/> copier.</param>
        public ValueTupleCopier(
            IDeepCopier<T1> copier1,
            IDeepCopier<T2> copier2,
            IDeepCopier<T3> copier3)
        {
            _copier1 = copier1;
            _copier2 = copier2;
            _copier3 = copier3;
        }

        /// <inheritdoc />
        public ValueTuple<T1, T2, T3> DeepCopy(ValueTuple<T1, T2, T3> input, CopyContext context) => ValueTuple.Create(
                _copier1.DeepCopy(input.Item1, context),
                _copier2.DeepCopy(input.Item2, context),
                _copier3.DeepCopy(input.Item3, context));
    }

    /// <summary>
    /// Serializer for <see cref="ValueTuple{T1, T2, T3, T4}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of the tuple's first component.</typeparam>
    /// <typeparam name="T2">The type of the tuple's second component.</typeparam>
    /// <typeparam name="T3">The type of the tuple's third component.</typeparam>
    /// <typeparam name="T4">The type of the tuple's fourth component.</typeparam>
    [RegisterSerializer]
    public sealed class ValueTupleCodec<T1, T2, T3, T4> : IFieldCodec<ValueTuple<T1, T2, T3, T4>>
    {
        private static readonly Type ElementType1 = typeof(T1);
        private static readonly Type ElementType2 = typeof(T2);
        private static readonly Type ElementType3 = typeof(T3);
        private static readonly Type ElementType4 = typeof(T4);

        private readonly IFieldCodec<T1> _item1Codec;
        private readonly IFieldCodec<T2> _item2Codec;
        private readonly IFieldCodec<T3> _item3Codec;
        private readonly IFieldCodec<T4> _item4Codec;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleCodec{T1, T2, T3, T4}"/> class.
        /// </summary>
        /// <param name="item1Codec">The <typeparamref name="T1"/> codec.</param>
        /// <param name="item2Codec">The <typeparamref name="T2"/> codec.</param>
        /// <param name="item3Codec">The <typeparamref name="T3"/> codec.</param>
        /// <param name="item4Codec">The <typeparamref name="T4"/> codec.</param>
        public ValueTupleCodec(
            IFieldCodec<T1> item1Codec,
            IFieldCodec<T2> item2Codec,
            IFieldCodec<T3> item3Codec,
            IFieldCodec<T4> item4Codec)
        {
            _item1Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item1Codec);
            _item2Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item2Codec);
            _item3Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item3Codec);
            _item4Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item4Codec);
        }

        /// <inheritdoc />
        void IFieldCodec<ValueTuple<T1, T2, T3, T4>>.WriteField<TBufferWriter>(
            ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            (T1, T2, T3, T4) value)
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            _item1Codec.WriteField(ref writer, 1, ElementType1, value.Item1);
            _item2Codec.WriteField(ref writer, 1, ElementType2, value.Item2);
            _item3Codec.WriteField(ref writer, 1, ElementType3, value.Item3);
            _item4Codec.WriteField(ref writer, 1, ElementType4, value.Item4);

            writer.WriteEndObject();
        }

        /// <inheritdoc />
        (T1, T2, T3, T4) IFieldCodec<ValueTuple<T1, T2, T3, T4>>.ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType != WireType.TagDelimited)
            {
                ValueTupleCodec.ThrowUnsupportedWireTypeException();
            }

            ReferenceCodec.MarkValueField(reader.Session);
            var item1 = default(T1);
            var item2 = default(T2);
            var item3 = default(T3);
            var item4 = default(T4);
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader();
                if (header.IsEndBaseOrEndObject)
                {
                    break;
                }

                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 1:
                        item1 = _item1Codec.ReadValue(ref reader, header);
                        break;
                    case 2:
                        item2 = _item2Codec.ReadValue(ref reader, header);
                        break;
                    case 3:
                        item3 = _item3Codec.ReadValue(ref reader, header);
                        break;
                    case 4:
                        item4 = _item4Codec.ReadValue(ref reader, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(header);
                        break;
                }
            }

            return new ValueTuple<T1, T2, T3, T4>(item1, item2, item3, item4);
        }
    }

    /// <summary>
    /// Copier for <see cref="ValueTuple{T1, T2, T3, T4}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of the tuple's first component.</typeparam>
    /// <typeparam name="T2">The type of the tuple's second component.</typeparam>
    /// <typeparam name="T3">The type of the tuple's third component.</typeparam>
    /// <typeparam name="T4">The type of the tuple's fourth component.</typeparam>
    [RegisterCopier]
    public class ValueTupleCopier<T1, T2, T3, T4> : IDeepCopier<ValueTuple<T1, T2, T3, T4>>
    {
        private readonly IDeepCopier<T1> _copier1;
        private readonly IDeepCopier<T2> _copier2;
        private readonly IDeepCopier<T3> _copier3;
        private readonly IDeepCopier<T4> _copier4;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleCopier{T1, T2, T3, T4}"/> class.
        /// </summary>
        /// <param name="copier1">The <typeparamref name="T1"/> copier.</param>
        /// <param name="copier2">The <typeparamref name="T2"/> copier.</param>
        /// <param name="copier3">The <typeparamref name="T3"/> copier.</param>
        /// <param name="copier4">The <typeparamref name="T4"/> copier.</param>
        public ValueTupleCopier(
            IDeepCopier<T1> copier1,
            IDeepCopier<T2> copier2,
            IDeepCopier<T3> copier3,
            IDeepCopier<T4> copier4)
        {
            _copier1 = copier1;
            _copier2 = copier2;
            _copier3 = copier3;
            _copier4 = copier4;
        }

        /// <inheritdoc />
        public ValueTuple<T1, T2, T3, T4> DeepCopy(ValueTuple<T1, T2, T3, T4> input, CopyContext context) => ValueTuple.Create(
                _copier1.DeepCopy(input.Item1, context),
                _copier2.DeepCopy(input.Item2, context),
                _copier3.DeepCopy(input.Item3, context),
                _copier4.DeepCopy(input.Item4, context));
    }

    /// <summary>
    /// Serializer for <see cref="ValueTuple{T1, T2, T3, T4, T5}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of the tuple's first component.</typeparam>
    /// <typeparam name="T2">The type of the tuple's second component.</typeparam>
    /// <typeparam name="T3">The type of the tuple's third component.</typeparam>
    /// <typeparam name="T4">The type of the tuple's fourth component.</typeparam>
    /// <typeparam name="T5">The type of the tuple's fifth component.</typeparam>
    [RegisterSerializer]
    public sealed class ValueTupleCodec<T1, T2, T3, T4, T5> : IFieldCodec<ValueTuple<T1, T2, T3, T4, T5>>
    {
        private static readonly Type ElementType1 = typeof(T1);
        private static readonly Type ElementType2 = typeof(T2);
        private static readonly Type ElementType3 = typeof(T3);
        private static readonly Type ElementType4 = typeof(T4);
        private static readonly Type ElementType5 = typeof(T5);

        private readonly IFieldCodec<T1> _item1Codec;
        private readonly IFieldCodec<T2> _item2Codec;
        private readonly IFieldCodec<T3> _item3Codec;
        private readonly IFieldCodec<T4> _item4Codec;
        private readonly IFieldCodec<T5> _item5Codec;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleCodec{T1, T2, T3, T4, T5}"/> class.
        /// </summary>
        /// <param name="item1Codec">The <typeparamref name="T1"/> codec.</param>
        /// <param name="item2Codec">The <typeparamref name="T2"/> codec.</param>
        /// <param name="item3Codec">The <typeparamref name="T3"/> codec.</param>
        /// <param name="item4Codec">The <typeparamref name="T4"/> codec.</param>
        /// <param name="item5Codec">The <typeparamref name="T5"/> codec.</param>
        public ValueTupleCodec(
            IFieldCodec<T1> item1Codec,
            IFieldCodec<T2> item2Codec,
            IFieldCodec<T3> item3Codec,
            IFieldCodec<T4> item4Codec,
            IFieldCodec<T5> item5Codec)
        {
            _item1Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item1Codec);
            _item2Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item2Codec);
            _item3Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item3Codec);
            _item4Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item4Codec);
            _item5Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item5Codec);
        }

        /// <inheritdoc />
        void IFieldCodec<ValueTuple<T1, T2, T3, T4, T5>>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            (T1, T2, T3, T4, T5) value)
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            _item1Codec.WriteField(ref writer, 1, ElementType1, value.Item1);
            _item2Codec.WriteField(ref writer, 1, ElementType2, value.Item2);
            _item3Codec.WriteField(ref writer, 1, ElementType3, value.Item3);
            _item4Codec.WriteField(ref writer, 1, ElementType4, value.Item4);
            _item5Codec.WriteField(ref writer, 1, ElementType5, value.Item5);

            writer.WriteEndObject();
        }

        /// <inheritdoc />
        (T1, T2, T3, T4, T5) IFieldCodec<ValueTuple<T1, T2, T3, T4, T5>>.ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType != WireType.TagDelimited)
            {
                ValueTupleCodec.ThrowUnsupportedWireTypeException();
            }

            ReferenceCodec.MarkValueField(reader.Session);
            var item1 = default(T1);
            var item2 = default(T2);
            var item3 = default(T3);
            var item4 = default(T4);
            var item5 = default(T5);
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader();
                if (header.IsEndBaseOrEndObject)
                {
                    break;
                }

                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 1:
                        item1 = _item1Codec.ReadValue(ref reader, header);
                        break;
                    case 2:
                        item2 = _item2Codec.ReadValue(ref reader, header);
                        break;
                    case 3:
                        item3 = _item3Codec.ReadValue(ref reader, header);
                        break;
                    case 4:
                        item4 = _item4Codec.ReadValue(ref reader, header);
                        break;
                    case 5:
                        item5 = _item5Codec.ReadValue(ref reader, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(header);
                        break;
                }
            }

            return new ValueTuple<T1, T2, T3, T4, T5>(item1, item2, item3, item4, item5);
        }
    }

    /// <summary>
    /// Copier for <see cref="ValueTuple{T1, T2, T3, T4, T5}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of the tuple's first component.</typeparam>
    /// <typeparam name="T2">The type of the tuple's second component.</typeparam>
    /// <typeparam name="T3">The type of the tuple's third component.</typeparam>
    /// <typeparam name="T4">The type of the tuple's fourth component.</typeparam>
    /// <typeparam name="T5">The type of the tuple's fifth component.</typeparam>
    [RegisterCopier]
    public class ValueTupleCopier<T1, T2, T3, T4, T5> : IDeepCopier<ValueTuple<T1, T2, T3, T4, T5>>
    {
        private readonly IDeepCopier<T1> _copier1;
        private readonly IDeepCopier<T2> _copier2;
        private readonly IDeepCopier<T3> _copier3;
        private readonly IDeepCopier<T4> _copier4;
        private readonly IDeepCopier<T5> _copier5;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleCopier{T1, T2, T3, T4, T5}"/> class.
        /// </summary>
        /// <param name="copier1">The <typeparamref name="T1"/> copier.</param>
        /// <param name="copier2">The <typeparamref name="T2"/> copier.</param>
        /// <param name="copier3">The <typeparamref name="T3"/> copier.</param>
        /// <param name="copier4">The <typeparamref name="T4"/> copier.</param>
        /// <param name="copier5">The <typeparamref name="T5"/> copier.</param>
        public ValueTupleCopier(
            IDeepCopier<T1> copier1,
            IDeepCopier<T2> copier2,
            IDeepCopier<T3> copier3,
            IDeepCopier<T4> copier4,
            IDeepCopier<T5> copier5)
        {
            _copier1 = copier1;
            _copier2 = copier2;
            _copier3 = copier3;
            _copier4 = copier4;
            _copier5 = copier5;
        }

        /// <inheritdoc />
        public ValueTuple<T1, T2, T3, T4, T5> DeepCopy(ValueTuple<T1, T2, T3, T4, T5> input, CopyContext context) => ValueTuple.Create(
                _copier1.DeepCopy(input.Item1, context),
                _copier2.DeepCopy(input.Item2, context),
                _copier3.DeepCopy(input.Item3, context),
                _copier4.DeepCopy(input.Item4, context),
                _copier5.DeepCopy(input.Item5, context));
    } 

    /// <summary>
    /// Serializer for <see cref="ValueTuple{T1, T2, T3, T4, T5, T6}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of the tuple's first component.</typeparam>
    /// <typeparam name="T2">The type of the tuple's second component.</typeparam>
    /// <typeparam name="T3">The type of the tuple's third component.</typeparam>
    /// <typeparam name="T4">The type of the tuple's fourth component.</typeparam>
    /// <typeparam name="T5">The type of the tuple's fifth component.</typeparam>
    /// <typeparam name="T6">The type of the tuple's sixth component.</typeparam>
    [RegisterSerializer]
    public sealed class ValueTupleCodec<T1, T2, T3, T4, T5, T6> : IFieldCodec<ValueTuple<T1, T2, T3, T4, T5, T6>>
    {
        private static readonly Type ElementType1 = typeof(T1);
        private static readonly Type ElementType2 = typeof(T2);
        private static readonly Type ElementType3 = typeof(T3);
        private static readonly Type ElementType4 = typeof(T4);
        private static readonly Type ElementType5 = typeof(T5);
        private static readonly Type ElementType6 = typeof(T6);

        private readonly IFieldCodec<T1> _item1Codec;
        private readonly IFieldCodec<T2> _item2Codec;
        private readonly IFieldCodec<T3> _item3Codec;
        private readonly IFieldCodec<T4> _item4Codec;
        private readonly IFieldCodec<T5> _item5Codec;
        private readonly IFieldCodec<T6> _item6Codec;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleCodec{T1, T2, T3, T4, T5, T6}"/> class.
        /// </summary>
        /// <param name="item1Codec">The <typeparamref name="T1"/> codec.</param>
        /// <param name="item2Codec">The <typeparamref name="T2"/> codec.</param>
        /// <param name="item3Codec">The <typeparamref name="T3"/> codec.</param>
        /// <param name="item4Codec">The <typeparamref name="T4"/> codec.</param>
        /// <param name="item5Codec">The <typeparamref name="T5"/> codec.</param>
        /// <param name="item6Codec">The <typeparamref name="T6"/> codec.</param>
        public ValueTupleCodec(
            IFieldCodec<T1> item1Codec,
            IFieldCodec<T2> item2Codec,
            IFieldCodec<T3> item3Codec,
            IFieldCodec<T4> item4Codec,
            IFieldCodec<T5> item5Codec,
            IFieldCodec<T6> item6Codec)
        {
            _item1Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item1Codec);
            _item2Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item2Codec);
            _item3Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item3Codec);
            _item4Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item4Codec);
            _item5Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item5Codec);
            _item6Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item6Codec);
        }

        /// <inheritdoc />
        void IFieldCodec<ValueTuple<T1, T2, T3, T4, T5, T6>>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            (T1, T2, T3, T4, T5, T6) value)
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            _item1Codec.WriteField(ref writer, 1, ElementType1, value.Item1);
            _item2Codec.WriteField(ref writer, 1, ElementType2, value.Item2);
            _item3Codec.WriteField(ref writer, 1, ElementType3, value.Item3);
            _item4Codec.WriteField(ref writer, 1, ElementType4, value.Item4);
            _item5Codec.WriteField(ref writer, 1, ElementType5, value.Item5);
            _item6Codec.WriteField(ref writer, 1, ElementType6, value.Item6);


            writer.WriteEndObject();
        }

        /// <inheritdoc />
        (T1, T2, T3, T4, T5, T6) IFieldCodec<ValueTuple<T1, T2, T3, T4, T5, T6>>.ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType != WireType.TagDelimited)
            {
                ValueTupleCodec.ThrowUnsupportedWireTypeException();
            }

            ReferenceCodec.MarkValueField(reader.Session);
            var item1 = default(T1);
            var item2 = default(T2);
            var item3 = default(T3);
            var item4 = default(T4);
            var item5 = default(T5);
            var item6 = default(T6);
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader();
                if (header.IsEndBaseOrEndObject)
                {
                    break;
                }

                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 1:
                        item1 = _item1Codec.ReadValue(ref reader, header);
                        break;
                    case 2:
                        item2 = _item2Codec.ReadValue(ref reader, header);
                        break;
                    case 3:
                        item3 = _item3Codec.ReadValue(ref reader, header);
                        break;
                    case 4:
                        item4 = _item4Codec.ReadValue(ref reader, header);
                        break;
                    case 5:
                        item5 = _item5Codec.ReadValue(ref reader, header);
                        break;
                    case 6:
                        item6 = _item6Codec.ReadValue(ref reader, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(header);
                        break;
                }
            }

            return new ValueTuple<T1, T2, T3, T4, T5, T6>(item1, item2, item3, item4, item5, item6);
        }
    }
    
    /// <summary>
    /// Copier for <see cref="ValueTuple{T1, T2, T3, T4, T5, T6}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of the tuple's first component.</typeparam>
    /// <typeparam name="T2">The type of the tuple's second component.</typeparam>
    /// <typeparam name="T3">The type of the tuple's third component.</typeparam>
    /// <typeparam name="T4">The type of the tuple's fourth component.</typeparam>
    /// <typeparam name="T5">The type of the tuple's fifth component.</typeparam>
    /// <typeparam name="T6">The type of the tuple's sixth component.</typeparam>
    [RegisterCopier]
    public class ValueTupleCopier<T1, T2, T3, T4, T5, T6> : IDeepCopier<ValueTuple<T1, T2, T3, T4, T5, T6>>
    {
        private readonly IDeepCopier<T1> _copier1;
        private readonly IDeepCopier<T2> _copier2;
        private readonly IDeepCopier<T3> _copier3;
        private readonly IDeepCopier<T4> _copier4;
        private readonly IDeepCopier<T5> _copier5;
        private readonly IDeepCopier<T6> _copier6;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleCopier{T1, T2, T3, T4, T5, T6}"/> class.
        /// </summary>
        /// <param name="copier1">The <typeparamref name="T1"/> copier.</param>
        /// <param name="copier2">The <typeparamref name="T2"/> copier.</param>
        /// <param name="copier3">The <typeparamref name="T3"/> copier.</param>
        /// <param name="copier4">The <typeparamref name="T4"/> copier.</param>
        /// <param name="copier5">The <typeparamref name="T5"/> copier.</param>
        /// <param name="copier6">The <typeparamref name="T6"/> copier.</param>
        public ValueTupleCopier(
            IDeepCopier<T1> copier1,
            IDeepCopier<T2> copier2,
            IDeepCopier<T3> copier3,
            IDeepCopier<T4> copier4,
            IDeepCopier<T5> copier5,
            IDeepCopier<T6> copier6)
        {
            _copier1 = copier1;
            _copier2 = copier2;
            _copier3 = copier3;
            _copier4 = copier4;
            _copier5 = copier5;
            _copier6 = copier6;
        }

        /// <inheritdoc />
        public ValueTuple<T1, T2, T3, T4, T5, T6> DeepCopy(ValueTuple<T1, T2, T3, T4, T5, T6> input, CopyContext context) => ValueTuple.Create(
                _copier1.DeepCopy(input.Item1, context),
                _copier2.DeepCopy(input.Item2, context),
                _copier3.DeepCopy(input.Item3, context),
                _copier4.DeepCopy(input.Item4, context),
                _copier5.DeepCopy(input.Item5, context),
                _copier6.DeepCopy(input.Item6, context));
    } 

    /// <summary>
    /// Serializer for <see cref="ValueTuple{T1, T2, T3, T4, T5, T6, T7}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of the tuple's first component.</typeparam>
    /// <typeparam name="T2">The type of the tuple's second component.</typeparam>
    /// <typeparam name="T3">The type of the tuple's third component.</typeparam>
    /// <typeparam name="T4">The type of the tuple's fourth component.</typeparam>
    /// <typeparam name="T5">The type of the tuple's fifth component.</typeparam>
    /// <typeparam name="T6">The type of the tuple's sixth component.</typeparam>
    /// <typeparam name="T7">The type of the tuple's seventh component.</typeparam>
    [RegisterSerializer]
    public sealed class ValueTupleCodec<T1, T2, T3, T4, T5, T6, T7> : IFieldCodec<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>
    {
        private static readonly Type ElementType1 = typeof(T1);
        private static readonly Type ElementType2 = typeof(T2);
        private static readonly Type ElementType3 = typeof(T3);
        private static readonly Type ElementType4 = typeof(T4);
        private static readonly Type ElementType5 = typeof(T5);
        private static readonly Type ElementType6 = typeof(T6);
        private static readonly Type ElementType7 = typeof(T7);

        private readonly IFieldCodec<T1> _item1Codec;
        private readonly IFieldCodec<T2> _item2Codec;
        private readonly IFieldCodec<T3> _item3Codec;
        private readonly IFieldCodec<T4> _item4Codec;
        private readonly IFieldCodec<T5> _item5Codec;
        private readonly IFieldCodec<T6> _item6Codec;
        private readonly IFieldCodec<T7> _item7Codec;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleCodec{T1, T2, T3, T4, T5, T6, T7}"/> class.
        /// </summary>
        /// <param name="item1Codec">The <typeparamref name="T1"/> codec.</param>
        /// <param name="item2Codec">The <typeparamref name="T2"/> codec.</param>
        /// <param name="item3Codec">The <typeparamref name="T3"/> codec.</param>
        /// <param name="item4Codec">The <typeparamref name="T4"/> codec.</param>
        /// <param name="item5Codec">The <typeparamref name="T5"/> codec.</param>
        /// <param name="item6Codec">The <typeparamref name="T6"/> codec.</param>
        /// <param name="item7Codec">The <typeparamref name="T7"/> codec.</param>
        public ValueTupleCodec(
            IFieldCodec<T1> item1Codec,
            IFieldCodec<T2> item2Codec,
            IFieldCodec<T3> item3Codec,
            IFieldCodec<T4> item4Codec,
            IFieldCodec<T5> item5Codec,
            IFieldCodec<T6> item6Codec,
            IFieldCodec<T7> item7Codec)
        {
            _item1Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item1Codec);
            _item2Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item2Codec);
            _item3Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item3Codec);
            _item4Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item4Codec);
            _item5Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item5Codec);
            _item6Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item6Codec);
            _item7Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item7Codec);
        }

        /// <inheritdoc />
        void IFieldCodec<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            (T1, T2, T3, T4, T5, T6, T7) value)
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            _item1Codec.WriteField(ref writer, 1, ElementType1, value.Item1);
            _item2Codec.WriteField(ref writer, 1, ElementType2, value.Item2);
            _item3Codec.WriteField(ref writer, 1, ElementType3, value.Item3);
            _item4Codec.WriteField(ref writer, 1, ElementType4, value.Item4);
            _item5Codec.WriteField(ref writer, 1, ElementType5, value.Item5);
            _item6Codec.WriteField(ref writer, 1, ElementType6, value.Item6);
            _item7Codec.WriteField(ref writer, 1, ElementType7, value.Item7);


            writer.WriteEndObject();
        }

        /// <inheritdoc />
        (T1, T2, T3, T4, T5, T6, T7) IFieldCodec<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>.ReadValue<TInput>(
            ref Reader<TInput> reader,
            Field field)
        {
            if (field.WireType != WireType.TagDelimited)
            {
                ValueTupleCodec.ThrowUnsupportedWireTypeException();
            }

            ReferenceCodec.MarkValueField(reader.Session);
            var item1 = default(T1);
            var item2 = default(T2);
            var item3 = default(T3);
            var item4 = default(T4);
            var item5 = default(T5);
            var item6 = default(T6);
            var item7 = default(T7);
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader();
                if (header.IsEndBaseOrEndObject)
                {
                    break;
                }

                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 1:
                        item1 = _item1Codec.ReadValue(ref reader, header);
                        break;
                    case 2:
                        item2 = _item2Codec.ReadValue(ref reader, header);
                        break;
                    case 3:
                        item3 = _item3Codec.ReadValue(ref reader, header);
                        break;
                    case 4:
                        item4 = _item4Codec.ReadValue(ref reader, header);
                        break;
                    case 5:
                        item5 = _item5Codec.ReadValue(ref reader, header);
                        break;
                    case 6:
                        item6 = _item6Codec.ReadValue(ref reader, header);
                        break;
                    case 7:
                        item7 = _item7Codec.ReadValue(ref reader, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(header);
                        break;
                }
            }

            return new ValueTuple<T1, T2, T3, T4, T5, T6, T7>(item1, item2, item3, item4, item5, item6, item7);
        }
    }

    /// <summary>
    /// Copier for <see cref="ValueTuple{T1, T2, T3, T4, T5, T6, T7}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of the tuple's first component.</typeparam>
    /// <typeparam name="T2">The type of the tuple's second component.</typeparam>
    /// <typeparam name="T3">The type of the tuple's third component.</typeparam>
    /// <typeparam name="T4">The type of the tuple's fourth component.</typeparam>
    /// <typeparam name="T5">The type of the tuple's fifth component.</typeparam>
    /// <typeparam name="T6">The type of the tuple's sixth component.</typeparam>
    /// <typeparam name="T7">The type of the tuple's seventh component.</typeparam>
    [RegisterCopier]
    public class ValueTupleCopier<T1, T2, T3, T4, T5, T6, T7> : IDeepCopier<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>
    {
        private readonly IDeepCopier<T1> _copier1;
        private readonly IDeepCopier<T2> _copier2;
        private readonly IDeepCopier<T3> _copier3;
        private readonly IDeepCopier<T4> _copier4;
        private readonly IDeepCopier<T5> _copier5;
        private readonly IDeepCopier<T6> _copier6;
        private readonly IDeepCopier<T7> _copier7;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleCopier{T1, T2, T3, T4, T5, T6, T7}"/> class.
        /// </summary>
        /// <param name="copier1">The <typeparamref name="T1"/> copier.</param>
        /// <param name="copier2">The <typeparamref name="T2"/> copier.</param>
        /// <param name="copier3">The <typeparamref name="T3"/> copier.</param>
        /// <param name="copier4">The <typeparamref name="T4"/> copier.</param>
        /// <param name="copier5">The <typeparamref name="T5"/> copier.</param>
        /// <param name="copier6">The <typeparamref name="T6"/> copier.</param>
        /// <param name="copier7">The <typeparamref name="T7"/> copier.</param>
        public ValueTupleCopier(
            IDeepCopier<T1> copier1,
            IDeepCopier<T2> copier2,
            IDeepCopier<T3> copier3,
            IDeepCopier<T4> copier4,
            IDeepCopier<T5> copier5,
            IDeepCopier<T6> copier6,
            IDeepCopier<T7> copier7)
        {
            _copier1 = copier1;
            _copier2 = copier2;
            _copier3 = copier3;
            _copier4 = copier4;
            _copier5 = copier5;
            _copier6 = copier6;
            _copier7 = copier7;
        }

        /// <inheritdoc />
        public ValueTuple<T1, T2, T3, T4, T5, T6, T7> DeepCopy(ValueTuple<T1, T2, T3, T4, T5, T6, T7> input, CopyContext context) => ValueTuple.Create(
                _copier1.DeepCopy(input.Item1, context),
                _copier2.DeepCopy(input.Item2, context),
                _copier3.DeepCopy(input.Item3, context),
                _copier4.DeepCopy(input.Item4, context),
                _copier5.DeepCopy(input.Item5, context),
                _copier6.DeepCopy(input.Item6, context),
                _copier7.DeepCopy(input.Item7, context));
    } 

    /// <summary>
    /// Serializer for <see cref="ValueTuple{T1, T2, T3, T4, T5, T6, T7, T8}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of the tuple's first component.</typeparam>
    /// <typeparam name="T2">The type of the tuple's second component.</typeparam>
    /// <typeparam name="T3">The type of the tuple's third component.</typeparam>
    /// <typeparam name="T4">The type of the tuple's fourth component.</typeparam>
    /// <typeparam name="T5">The type of the tuple's fifth component.</typeparam>
    /// <typeparam name="T6">The type of the tuple's sixth component.</typeparam>
    /// <typeparam name="T7">The type of the tuple's seventh component.</typeparam>
    /// <typeparam name="T8">The type of the tuple's eighth component.</typeparam>
    [RegisterSerializer]
    public sealed class ValueTupleCodec<T1, T2, T3, T4, T5, T6, T7, T8> : IFieldCodec<ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8>> where T8 : struct
    {
        private static readonly Type ElementType1 = typeof(T1);
        private static readonly Type ElementType2 = typeof(T2);
        private static readonly Type ElementType3 = typeof(T3);
        private static readonly Type ElementType4 = typeof(T4);
        private static readonly Type ElementType5 = typeof(T5);
        private static readonly Type ElementType6 = typeof(T6);
        private static readonly Type ElementType7 = typeof(T7);
        private static readonly Type ElementType8 = typeof(T8);

        private readonly IFieldCodec<T1> _item1Codec;
        private readonly IFieldCodec<T2> _item2Codec;
        private readonly IFieldCodec<T3> _item3Codec;
        private readonly IFieldCodec<T4> _item4Codec;
        private readonly IFieldCodec<T5> _item5Codec;
        private readonly IFieldCodec<T6> _item6Codec;
        private readonly IFieldCodec<T7> _item7Codec;
        private readonly IFieldCodec<T8> _item8Codec;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleCodec{T1, T2, T3, T4, T5, T6, T7, T8}"/> class.
        /// </summary>
        /// <param name="item1Codec">The <typeparamref name="T1"/> codec.</param>
        /// <param name="item2Codec">The <typeparamref name="T2"/> codec.</param>
        /// <param name="item3Codec">The <typeparamref name="T3"/> codec.</param>
        /// <param name="item4Codec">The <typeparamref name="T4"/> codec.</param>
        /// <param name="item5Codec">The <typeparamref name="T5"/> codec.</param>
        /// <param name="item6Codec">The <typeparamref name="T6"/> codec.</param>
        /// <param name="item7Codec">The <typeparamref name="T7"/> codec.</param>
        /// <param name="item8Codec">The <typeparamref name="T8"/> codec.</param>
        public ValueTupleCodec(
            IFieldCodec<T1> item1Codec,
            IFieldCodec<T2> item2Codec,
            IFieldCodec<T3> item3Codec,
            IFieldCodec<T4> item4Codec,
            IFieldCodec<T5> item5Codec,
            IFieldCodec<T6> item6Codec,
            IFieldCodec<T7> item7Codec,
            IFieldCodec<T8> item8Codec)
        {
            _item1Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item1Codec);
            _item2Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item2Codec);
            _item3Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item3Codec);
            _item4Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item4Codec);
            _item5Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item5Codec);
            _item6Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item6Codec);
            _item7Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item7Codec);
            _item8Codec = OrleansGeneratedCodeHelper.UnwrapService(this, item8Codec);
        }

        /// <inheritdoc />
        void IFieldCodec<ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8>>.WriteField<TBufferWriter>(
            ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8> value)
        {
            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

            _item1Codec.WriteField(ref writer, 1, ElementType1, value.Item1);
            _item2Codec.WriteField(ref writer, 1, ElementType2, value.Item2);
            _item3Codec.WriteField(ref writer, 1, ElementType3, value.Item3);
            _item4Codec.WriteField(ref writer, 1, ElementType4, value.Item4);
            _item5Codec.WriteField(ref writer, 1, ElementType5, value.Item5);
            _item6Codec.WriteField(ref writer, 1, ElementType6, value.Item6);
            _item7Codec.WriteField(ref writer, 1, ElementType7, value.Item7);
            _item8Codec.WriteField(ref writer, 1, ElementType8, value.Rest);

            writer.WriteEndObject();
        }

        /// <inheritdoc />
        ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8> IFieldCodec<ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8>>.ReadValue<TInput>(ref Reader<TInput> reader,
            Field field)
        {
            if (field.WireType != WireType.TagDelimited)
            {
                ValueTupleCodec.ThrowUnsupportedWireTypeException();
            }

            ReferenceCodec.MarkValueField(reader.Session);
            var item1 = default(T1);
            var item2 = default(T2);
            var item3 = default(T3);
            var item4 = default(T4);
            var item5 = default(T5);
            var item6 = default(T6);
            var item7 = default(T7);
            var item8 = default(T8);
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader();
                if (header.IsEndBaseOrEndObject)
                {
                    break;
                }

                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 1:
                        item1 = _item1Codec.ReadValue(ref reader, header);
                        break;
                    case 2:
                        item2 = _item2Codec.ReadValue(ref reader, header);
                        break;
                    case 3:
                        item3 = _item3Codec.ReadValue(ref reader, header);
                        break;
                    case 4:
                        item4 = _item4Codec.ReadValue(ref reader, header);
                        break;
                    case 5:
                        item5 = _item5Codec.ReadValue(ref reader, header);
                        break;
                    case 6:
                        item6 = _item6Codec.ReadValue(ref reader, header);
                        break;
                    case 7:
                        item7 = _item7Codec.ReadValue(ref reader, header);
                        break;
                    case 8:
                        item8 = _item8Codec.ReadValue(ref reader, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(header);
                        break;
                }
            }

            return new ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8>(item1, item2, item3, item4, item5, item6, item7, item8);
        }
    }

    /// <summary>
    /// Copier for <see cref="ValueTuple{T1, T2, T3, T4, T5, T6, T7, T8}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of the tuple's first component.</typeparam>
    /// <typeparam name="T2">The type of the tuple's second component.</typeparam>
    /// <typeparam name="T3">The type of the tuple's third component.</typeparam>
    /// <typeparam name="T4">The type of the tuple's fourth component.</typeparam>
    /// <typeparam name="T5">The type of the tuple's fifth component.</typeparam>
    /// <typeparam name="T6">The type of the tuple's sixth component.</typeparam>
    /// <typeparam name="T7">The type of the tuple's seventh component.</typeparam>
    /// <typeparam name="T8">The type of the tuple's eighth component.</typeparam>
    [RegisterCopier]
    public class ValueTupleCopier<T1, T2, T3, T4, T5, T6, T7, T8> : IDeepCopier<ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8>> where T8 : struct
    {
        private readonly IDeepCopier<T1> _copier1;
        private readonly IDeepCopier<T2> _copier2;
        private readonly IDeepCopier<T3> _copier3;
        private readonly IDeepCopier<T4> _copier4;
        private readonly IDeepCopier<T5> _copier5;
        private readonly IDeepCopier<T6> _copier6;
        private readonly IDeepCopier<T7> _copier7;
        private readonly IDeepCopier<T8> _copier8;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleCopier{T1, T2, T3, T4, T5, T6, T7, T8}"/> class.
        /// </summary>
        /// <param name="copier1">The <typeparamref name="T1"/> copier.</param>
        /// <param name="copier2">The <typeparamref name="T2"/> copier.</param>
        /// <param name="copier3">The <typeparamref name="T3"/> copier.</param>
        /// <param name="copier4">The <typeparamref name="T4"/> copier.</param>
        /// <param name="copier5">The <typeparamref name="T5"/> copier.</param>
        /// <param name="copier6">The <typeparamref name="T6"/> copier.</param>
        /// <param name="copier7">The <typeparamref name="T7"/> copier.</param>
        /// <param name="copier8">The <typeparamref name="T8"/> copier.</param>
        public ValueTupleCopier(
            IDeepCopier<T1> copier1,
            IDeepCopier<T2> copier2,
            IDeepCopier<T3> copier3,
            IDeepCopier<T4> copier4,
            IDeepCopier<T5> copier5,
            IDeepCopier<T6> copier6,
            IDeepCopier<T7> copier7,
            IDeepCopier<T8> copier8)
        {
            _copier1 = copier1;
            _copier2 = copier2;
            _copier3 = copier3;
            _copier4 = copier4;
            _copier5 = copier5;
            _copier6 = copier6;
            _copier7 = copier7;
            _copier8 = copier8;
        }

        /// <inheritdoc />
        public ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8> DeepCopy(ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8> input, CopyContext context) => new(
                _copier1.DeepCopy(input.Item1, context),
                _copier2.DeepCopy(input.Item2, context),
                _copier3.DeepCopy(input.Item3, context),
                _copier4.DeepCopy(input.Item4, context),
                _copier5.DeepCopy(input.Item5, context),
                _copier6.DeepCopy(input.Item6, context),
                _copier7.DeepCopy(input.Item7, context),
                _copier8.DeepCopy(input.Rest, context));
    } 
}
