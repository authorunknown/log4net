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
using System.Text.RegularExpressions;

using log4net;
using log4net.Core;
using log4net.Util;

namespace log4net.Filter
{
	/// <summary>
	/// Simple filter to match a string in the <see cref="MDC"/>
	/// </summary>
	/// <remarks>
	/// Simple filter to match a string in the <see cref="MDC"/>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class MdcFilter : FilterSkeleton
	{
		#region Member Variables

		/// <summary>
		/// Flag to indicate the behaviour when we have a match
		/// </summary>
		private bool m_acceptOnMatch = true;

		/// <summary>
		/// The string to substring match against the message
		/// </summary>
		private string m_stringToMatch;

		/// <summary>
		/// A string regex to match
		/// </summary>
		private string m_stringRegexToMatch;

		/// <summary>
		/// A regex object to match (generated from m_stringRegexToMatch)
		/// </summary>
		private Regex m_regexToMatch;

		/// <summary>
		/// The key to use to lookup the string
		/// from the MDC
		/// </summary>
		private string m_key;

		#endregion

		#region Constructors

		/// <summary>
		/// Default constructor
		/// </summary>
		public MdcFilter()
		{
		}

		#endregion

		#region Implementation of IOptionHandler

		/// <summary>
		/// Initialise and precompile the Regex if required
		/// </summary>
		override public void ActivateOptions() 
		{
			if (m_stringRegexToMatch != null)
			{
				m_regexToMatch = new Regex(m_stringRegexToMatch, RegexOptions.Compiled);
			}
		}

		#endregion

		/// <summary>
		/// The <see cref="AcceptOnMatch"/> property is a flag that determines
		/// the behaviour when a matching <see cref="Level"/> is found. If the
		/// flag is set to true then the filter will <see cref="FilterDecision.Accept"/> the 
		/// logging event, otherwise it will <see cref="FilterDecision.Deny"/> the event.
		/// </summary>
		public bool AcceptOnMatch
		{
			get { return m_acceptOnMatch; }
			set { m_acceptOnMatch = value; }
		}

		/// <summary>
		/// The string that will be substring matched against
		/// the rendered message. If the message contains this
		/// string then the filter will match.
		/// </summary>
		public string StringToMatch
		{
			get { return m_stringToMatch; }
			set { m_stringToMatch = value; }
		}

		/// <summary>
		/// The regular expression pattern that will be matched against
		/// the rendered message. If the message matches this
		/// pattern then the filter will match.
		/// </summary>
		public string RegexToMatch
		{
			get { return m_stringRegexToMatch; }
			set { m_stringRegexToMatch = value; }
		}

		/// <summary>
		/// The key to lookup in the <see cref="MDC"/> and then
		/// match against.
		/// </summary>
		public string Key
		{
			get { return m_key; }
			set { m_key = value; }
		}

		#region Override implementation of FilterSkeleton

		/// <summary>
		/// Check if this filter should allow the event to be logged
		/// </summary>
		/// <remarks>
		/// The <see cref="MDC"/> is matched against the <see cref="StringToMatch"/>.
		/// If the <see cref="StringToMatch"/> occurs as a substring within
		/// the message then a match will have occurred. If no match occurs
		/// this function will return <see cref="FilterDecision.Neutral"/>
		/// allowing other filters to check the event. If a match occurs then
		/// the value of <see cref="AcceptOnMatch"/> is checked. If it is
		/// true then <see cref="FilterDecision.Accept"/> is returned otherwise
		/// <see cref="FilterDecision.Deny"/> is returned.
		/// </remarks>
		/// <param name="loggingEvent">the event being logged</param>
		/// <returns>see remarks</returns>
		override public FilterDecision Decide(LoggingEvent loggingEvent) 
		{
			if (loggingEvent == null)
			{
				throw new ArgumentNullException("loggingEvent");
			}

			// Check if we have a key to lookup the MDC value with
			if (m_key == null)
			{
				// We cannot filter so allow the filter chain
				// to continue processing
				return FilterDecision.Neutral;
			}

			// Lookup the string to match in from the MDC using 
			// the key specified.
			string msg = loggingEvent.LookupMappedContext(m_key);

			// Check if we have been setup to filter
			if (msg == null || (m_stringToMatch == null && m_regexToMatch == null))
			{
				// We cannot filter so allow the filter chain
				// to continue processing
				return FilterDecision.Neutral;
			}
    
			// Firstly check if we are matching using a regex
			if (m_regexToMatch != null)
			{
				// Check the regex
				if (m_regexToMatch.Match(msg).Success == false)
				{
					// No match, continue processing
					return FilterDecision.Neutral;
				} 

				// we've got a match
				if (m_acceptOnMatch) 
				{
					return FilterDecision.Accept;
				} 
				return FilterDecision.Deny;
			}
			else if (m_stringToMatch != null)
			{
				// Check substring match
				if (msg.IndexOf(m_stringToMatch) == -1) 
				{
					// No match, continue processing
					return FilterDecision.Neutral;
				} 

				// we've got a match
				if (m_acceptOnMatch) 
				{
					return FilterDecision.Accept;
				} 
				return FilterDecision.Deny;
			}
			return FilterDecision.Neutral;
		}

		#endregion
	}
}