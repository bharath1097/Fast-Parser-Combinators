﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;

namespace ParserGenerator.Blittable {
    public sealed class UnsafeBlittableStructParser<T> : IParser<T> {
        private readonly int _length;
        private readonly UnsafeBlitUtil.UnsafeValueBlitParser<T> _parser;
        public UnsafeBlittableStructParser(IReadOnlyList<IFieldParserOfUnknownType> fieldParsers) {
            if (!IsBlitParsableBy(fieldParsers)) throw new ArgumentException("!IsBlitParsableBy(fieldParsers)", "fieldParsers");
            _parser = UnsafeBlitUtil.MakeUnsafeValueBlitParser<T>();
            _length = fieldParsers.Aggregate(0, (a, e) => a + e.OptionalConstantSerializedLength.Value);
        }

        public ParsedValue<T> Parse(ArraySegment<byte> data) {
            if (data.Count < _length) throw new InvalidOperationException("Fragment");
            var relevantData = new ArraySegment<byte>(data.Array, data.Offset, _length);
            var value = _parser(relevantData);
            return new ParsedValue<T>(value, _length);
        }
        public bool IsBlittable { get { return true; } }
        public int? OptionalConstantSerializedLength { get { return _length; } }

        public static bool IsBlitParsableBy(IReadOnlyList<IFieldParserOfUnknownType> fieldParsers) {
            if (fieldParsers == null) throw new ArgumentNullException("fieldParsers");

            // type has blittable representation?
            if (!Util.IsBlittable<T>()) return false;

            // all parsers have same constant length representation as value in memory?
            if (fieldParsers.Any(e => !e.IsBlittable)) return false;
            if (fieldParsers.Any(e => !e.OptionalConstantSerializedLength.HasValue)) return false;

            // type has no padding?
            var structLayout = typeof(T).StructLayoutAttribute;
            if (structLayout == null) return false;
            if (structLayout.Value != LayoutKind.Sequential) return false;
            if (structLayout.Pack != 1) return false;

            // parsers and struct fields have matching canonical names?
            var serialNames = fieldParsers.Select(e => e.CanonicalName());
            var fieldNames = typeof(T).GetFields().Select(e => e.CanonicalName());
            if (!serialNames.HasSameSetOfItemsAs(fieldNames)) return false;

            // offsets implied by parser ordering matches offsets of the struct's fields?
            var memoryOffsets =
                typeof(T).GetFields().ToDictionary(
                    e => e.CanonicalName(),
                    e => typeof(T).FieldOffsetOf(e));
            var serialOffsets =
                fieldParsers
                    .StreamZip(0, (a, e) => a + e.OptionalConstantSerializedLength.Value)
                    .ToDictionary(e => e.Item1.CanonicalName(), e => e.Item2 - e.Item1.OptionalConstantSerializedLength.Value);
            if (!serialOffsets.HasSameKeyValuesAs(memoryOffsets)) return false;

            return true;
        }
    }
}