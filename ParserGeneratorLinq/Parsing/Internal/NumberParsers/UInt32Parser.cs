﻿using System;
using System.Linq.Expressions;

namespace Strilanc.Parsing.Internal.NumberParsers {
    internal struct UInt32Parser : IParserInternal<UInt32> {
        private const int SerializedLength = 32 / 8;

        private readonly bool _isSystemEndian;
        public bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch { get { return _isSystemEndian; } }
        public int? OptionalConstantSerializedLength { get { return SerializedLength; } }

        public UInt32Parser(Endianess endianess) {
            if (endianess != Endianess.BigEndian && endianess != Endianess.LittleEndian)
                throw new ArgumentException("Unrecognized endianess", "endianess");
            var isLittleEndian = endianess == Endianess.LittleEndian;
            _isSystemEndian = isLittleEndian == BitConverter.IsLittleEndian;
        }

        public ParsedValue<UInt32> Parse(ArraySegment<byte> data) {
            var value = BitConverter.ToUInt32(data.Array, data.Offset);
            if (!_isSystemEndian) value = value.ReverseBytes();
            return new ParsedValue<UInt32>(value, SerializedLength);
        }

        public Tuple<Expression, ParameterExpression[]> TryMakeParseFromDataExpression(Expression array, Expression offset, Expression count) {
            return NumberParseBuilderUtil.MakeParseFromDataExpression<UInt32>(_isSystemEndian, array, offset, count);
        }
        public Expression TryMakeGetValueFromParsedExpression(Expression parsed) {
            return NumberParseBuilderUtil.MakeGetValueFromParsedExpression(parsed);
        }
        public Expression TryMakeGetConsumedFromParsedExpression(Expression parsed) {
            return NumberParseBuilderUtil.MakeGetConsumedFromParsedExpression<UInt32>(parsed);
        }
    }
}
