﻿#region Copyright and license information
// Copyright 2001-2009 Stephen Colebourne
// Copyright 2009-2011 Jon Skeet
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Globalization;
using NodaTime.Properties;

namespace NodaTime.Text
{
    internal class ParseResult<T>
    {
        private readonly T value;
        private readonly NodaFunc<Exception> exceptionProvider;
        private readonly bool continueWithMultiple;

        private ParseResult(NodaFunc<Exception> exceptionProvider, bool continueWithMultiple)
        {
            this.exceptionProvider = exceptionProvider;
            this.continueWithMultiple = continueWithMultiple;
        }

        private ParseResult(T value)
        {
            this.value = value;
        }

        internal T GetResultOrThrow()
        {
            if (exceptionProvider == null)
            {
                return value;
            }
            throw exceptionProvider();
        }

        /// <summary>
        /// Returns the success value, and sets the out parameter to either
        /// the specified failure value of T or the successful parse result value.
        /// </summary>
        internal bool TryGetResult(T failureValue, out T result)
        {
            bool success = exceptionProvider == null;
            result = success ? value : failureValue;
            return success;
        }

        internal static ParseResult<T> ForValue(T value)
        {
            return new ParseResult<T>(value);
        }

        internal bool Success { get { return exceptionProvider == null; } }

        internal bool ContinueAfterErrorWithMultipleFormats { get { return continueWithMultiple; } }

        #region Factory methods and readonly static fields

        internal static ParseResult<T> ForInvalidFormat(string formatString, params object[] parameters)
        {
            return ForInvalidFormat(() => new InvalidPatternException(string.Format(CultureInfo.CurrentCulture, formatString, parameters)));
        }

        internal static ParseResult<T> ForInvalidFormat(NodaFunc<Exception> exceptionProvider)
        {
            return new ParseResult<T>(exceptionProvider, false);
        }

        internal static ParseResult<T> ForInvalidValue(string formatString, params object[] parameters)
        {
            return ForInvalidValue(() => new UnparsableValueException(string.Format(CultureInfo.CurrentCulture, formatString, parameters)));
        }

        private static ParseResult<T> ForInvalidValue(NodaFunc<Exception> exceptionProvider)
        {
            return new ParseResult<T>(exceptionProvider, true);
        }

        internal static ParseResult<T> ArgumentNull(string parameter)
        {
            return new ParseResult<T>(() => new ArgumentNullException(parameter), false);
        }

        internal static readonly ParseResult<T> PositiveSignInvalid = ForInvalidValue(Resources.Parse_PositiveSignInvalid);

        internal static ParseResult<T> CannotParseValue(string value, string format)
        {
            return ForInvalidValue(Resources.Parse_CannotParseValue, value, typeof(T), format);
        }

        internal static ParseResult<T> DoubleAssigment(char patternCharacter)
        {
            return ForInvalidFormat(Resources.Parse_DoubleAssignment, patternCharacter);
        }

        // Special case: it's a fault with the value, but we still don't want to continue with multiple patterns.
        internal static readonly ParseResult<T> ValueStringEmpty =
            new ParseResult<T>(() => new UnparsableValueException(string.Format(CultureInfo.CurrentCulture, Resources.Parse_ValueStringEmpty)), false);

        internal static ParseResult<T> ExtraValueCharacters(string remainder)
        {
            return ForInvalidValue(Resources.Parse_ExtraValueCharacters, remainder);
        }

        // TODO: This should be ForInvalidValue
        internal static readonly ParseResult<T> QuotedStringMismatch = ForInvalidValue(Resources.Parse_QuotedStringMismatch);

        internal static ParseResult<T> EscapedCharacterMismatch(char patternCharacter)
        {
            return ForInvalidValue(Resources.Parse_EscapedCharacterMismatch, patternCharacter);
        }

        internal static ParseResult<T> MissingDecimalSeparator = ForInvalidValue(Resources.Parse_MissingDecimalSeparator);

        internal static ParseResult<T> TimeSeparatorMismatch = ForInvalidValue(Resources.Parse_TimeSeparatorMismatch);

        internal static ParseResult<T> MismatchedNumber(string pattern)
        {
            return ForInvalidValue(Resources.Parse_MismatchedNumber, pattern);
        }

        internal static ParseResult<T> MismatchedSpace = ForInvalidValue(Resources.Parse_MismatchedSpace);

        internal static ParseResult<T> MismatchedCharacter(char patternCharacter)
        {
            return ForInvalidValue(Resources.Parse_MismatchedCharacter, patternCharacter);
        }

        internal static readonly ParseResult<T> NoMatchingFormat = ForInvalidValue(Resources.Parse_NoMatchingFormat);

        internal static ParseResult<T> ValueOutOfRange(object value)
        {
            return ForInvalidValue(Resources.Parse_ValueOutOfRange, value, typeof(T));
        }

        internal static readonly ParseResult<T> MissingSign = ForInvalidValue(Resources.Parse_MissingSign);

        internal static ParseResult<T> FieldValueOutOfRange(object value, char field)
        {
            return ForInvalidValue(Resources.Parse_FieldValueOutOfRange, value, field, typeof(T));
        }
        #endregion
    }
}