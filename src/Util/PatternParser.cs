#region Copyright & License
//
// Copyright 2001-2004 The Apache Software Foundation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

using log4net.Layout;
using log4net.Core;
using log4net.DateFormatter;
using log4net.Util;

namespace log4net.Util
{
	/// <summary>
	/// Most of the work of the <see cref="PatternLayout"/> class
	/// is delegated to the PatternParser class.
	/// </summary>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class PatternParser
	{
		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="PatternParser" /> class 
		/// with the specified pattern string.
		/// </summary>
		/// <param name="pattern">The pattern to parse.</param>
		public PatternParser(string pattern) 
		{
			m_pattern = pattern;
		}

		#endregion Public Instance Constructors

		#region Public Instance Methods

		/// <summary>
		/// Parses the pattern into a chain of pattern converters.
		/// </summary>
		/// <returns>The head of a chain of pattern converters.</returns>
		public PatternConverter Parse()
		{
			string[] converterNamesCache = BuildCache();

			ParseInternal(m_pattern, converterNamesCache);

			return m_head;
		}

		#endregion Public Instance Methods

		#region Public Instance Properties

		/// <summary>
		/// The converter registry used by this parser
		/// </summary>
		public Hashtable ConverterRegistry
		{
			get { return m_converterRegistry; }
		}

		#endregion Public Instance Properties

		#region Protected Instance Methods

		/// <summary>
		/// Build the unified cache of converters from the static and instance maps
		/// </summary>
		/// <returns>the list of all the converter names</returns>
		protected string[] BuildCache()
		{
			string[] converterNamesCache = new string[m_converterRegistry.Keys.Count];
			m_converterRegistry.Keys.CopyTo(converterNamesCache, 0);

			// sort array so that longer strings come first
			Array.Sort(converterNamesCache, 0, converterNamesCache.Length, StringLengthComparer.Instance);

			return converterNamesCache;
		}

		/// <summary>
		/// IComparer that orders strings by string length.
		/// The longest strings are placed first
		/// </summary>
		private sealed class StringLengthComparer : IComparer
		{
			public static readonly StringLengthComparer Instance = new StringLengthComparer();

			private StringLengthComparer()
			{
			}

			#region Implementation of IComparer

			public int Compare(object x, object y)
			{
				string s1 = x as string;
				string s2 = y as string;

				if (s1 == null && s2 == null)
				{
					return 0;
				}
				if (s1 == null)
				{
					return 1;
				}
				if (s2 == null)
				{
					return -1;
				}

				return s2.Length.CompareTo(s1.Length);
			}
		
			#endregion
		}

		/// <summary>
		/// Internal method to parse the specified pattern to find specified matches
		/// </summary>
		/// <param name="pattern">the pattern to parse</param>
		/// <param name="matches">the converter names to match in the pattern</param>
		/// <remarks>
		/// The matches param must be sorted such that longer strings come before shorter ones.
		/// </remarks>
		protected void ParseInternal(string pattern, string[] matches)
		{
			int offset = 0;
			while(offset < pattern.Length)
			{
				int i = pattern.IndexOf('%', offset);
				if (i < 0 || i == pattern.Length - 1)
				{
					ProcessLiteral(pattern.Substring(offset));
					offset = pattern.Length;
				}
				else
				{
					if (pattern[i+1] == '%')
					{
						// Escaped
						ProcessLiteral(pattern.Substring(offset, i - offset + 1));
						offset = i + 2;
					}
					else
					{
						ProcessLiteral(pattern.Substring(offset, i - offset));
						offset = i + 1;

						FormattingInfo formattingInfo = new FormattingInfo();

						// Process formatting options

						// Look for the align flag
						if (offset < pattern.Length)
						{
							if (pattern[offset] == '-')
							{
								// Seen align flag
								formattingInfo.LeftAlign = true;
								offset++;
							}
						}
						// Look for the minimum length
						while (offset < pattern.Length && char.IsDigit(pattern[offset]))
						{
							// Seen digit
							if (formattingInfo.Min < 0)
							{
								formattingInfo.Min = 0;
							}
							formattingInfo.Min = (formattingInfo.Min * 10) + int.Parse(pattern[offset].ToString(CultureInfo.InvariantCulture), System.Globalization.NumberFormatInfo.InvariantInfo);
							offset++;
						}
						// Look for the seperator between min and max
						if (offset < pattern.Length)
						{
							if (pattern[offset] == '.')
							{
								// Seen seperator
								offset++;
							}
						}
						// Look for the maximum length
						while (offset < pattern.Length && char.IsDigit(pattern[offset]))
						{
							// Seen digit
							if (formattingInfo.Max == int.MaxValue)
							{
								formattingInfo.Max = 0;
							}
							formattingInfo.Max = (formattingInfo.Max * 10) + int.Parse(pattern[offset].ToString(CultureInfo.InvariantCulture), System.Globalization.NumberFormatInfo.InvariantInfo);
							offset++;
						}

						int remaingStringLength = pattern.Length - offset;

						// Look for pattern
						for(int m=0; m<matches.Length; m++)
						{
							if (matches[m].Length <= remaingStringLength)
							{
								if (String.Compare(pattern, offset, matches[m], 0, matches[m].Length, false, System.Globalization.CultureInfo.InvariantCulture) == 0)
								{
									// Found match
									offset = offset + matches[m].Length;

									string option = null;

									// Look for option
									if (offset < pattern.Length)
									{
										if (pattern[offset] == '{')
										{
											// Seen option start
											offset++;
											
											int optEnd = pattern.IndexOf('}', offset);
											if (optEnd < 0)
											{
												// error
											}
											else
											{
												option = pattern.Substring(offset, optEnd - offset);
												offset = optEnd + 1;
											}
										}
									}

									ProcessConverter(matches[m], option, formattingInfo);
									break;
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Process a parsed literal
		/// </summary>
		/// <param name="text">the literal text</param>
		protected void ProcessLiteral(string text)
		{
			if (text.Length > 0)
			{
				// Convert into a pattern
				ProcessConverter("literal", text, new FormattingInfo());
			}
		}

		/// <summary>
		/// Process a parsed converter pattern
		/// </summary>
		/// <param name="converterName">the name of the converter</param>
		/// <param name="option">the optional option for the converter</param>
		/// <param name="formattingInfo">the formatting info for the converter</param>
		protected void ProcessConverter(string converterName, string option, FormattingInfo formattingInfo)
		{
			LogLog.Debug("Converter: ["+converterName+"] Option: ["+option+"] Format: [min="+formattingInfo.Min+",max="+formattingInfo.Max+",leftAlign="+formattingInfo.LeftAlign+"]");

			// Lookup the converter type
			Type converterType = (Type)m_converterRegistry[converterName];
			if (converterType == null)
			{
				LogLog.Error("PatternParser: Unknown converter name ["+converterName+"] in conversion pattern.");
			}
			else
			{
				// Create the pattern converter
				ConstructorInfo constructor = converterType.GetConstructor(SystemInfo.EmptyTypes);
				if (constructor == null)
				{
					LogLog.Error("PatternParser: Converter Type ["+converterType.FullName+"] does not have a default constructor.");
				}
				else
				{
					PatternConverter pc = (PatternConverter)constructor.Invoke(BindingFlags.Public | BindingFlags.Instance, null, new object[0], CultureInfo.InvariantCulture);

					// formattingInfo variable is an instance variable, occasionally reset 
					// and used over and over again
					pc.FormattingInfo = formattingInfo;
					pc.Option = option;

					if (pc is IOptionHandler)
					{
						((IOptionHandler)pc).ActivateOptions();
					}

					AddConverter(pc);
				}
			}
		}

		/// <summary>
		/// Resets the internal state of the parser and adds the specified pattern converter 
		/// to the chain.
		/// </summary>
		/// <param name="pc">The pattern converter to add.</param>
		protected void AddConverter(PatternConverter pc) 
		{
			// Add the pattern converter to the list.

			if (m_head == null) 
			{
				m_head = m_tail = pc;
			}
			else 
			{
				// Set the next converter on the tail
				// Update the tail reference
				// note that a converter may combine the 'next' into itself
				// and therefore the tail would not change!
				m_tail = m_tail.SetNext(pc);
			}
		}

		#endregion Protected Instance Methods

		#region Private Constants

		private const char ESCAPE_CHAR = '%';
  
		#endregion Private Constants

		#region Private Instance Fields

		/// <summary>
		/// The first pattern converter in the chain
		/// </summary>
		private PatternConverter m_head;

		/// <summary>
		///  the last pattern converter in the chain
		/// </summary>
		private PatternConverter m_tail;

		/// <summary>
		/// The pattern
		/// </summary>
		private string m_pattern;

		/// <summary>
		/// Internal map of converter identifiers to converter types.
		/// </summary>
		/// <remarks>
		/// This map overrides the static s_globalRulesRegistry map
		/// </remarks>
		private Hashtable m_converterRegistry = new Hashtable();

		#endregion Private Instance Fields
	}
}