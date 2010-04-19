#region Apache License
//
// Licensed to the Apache Software Foundation (ASF) under one or more 
// contributor license agreements. See the NOTICE file distributed with
// this work for additional information regarding copyright ownership. 
// The ASF licenses this file to you under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with 
// the License. You may obtain a copy of the License at
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
using System.IO;
using System.Text.RegularExpressions;

using log4net.ObjectRenderer;

using NUnit.Framework;

namespace log4net.Tests.ObjectRenderer
{
    [TestFixture]
    public class DefaultRendererTest
    {
        RendererMap map;
        DefaultRenderer renderer;

        [SetUp]
        public void SetUp()
        {
            map = new RendererMap();
            renderer = new DefaultRenderer();
            map.Put(typeof(Object), renderer);
        }

        [Test]
        public void exception_type_name_and_namespace_should_be_rendered()
        {
            string renderedText = RenderException(new DummyException());

            Assert.That(renderedText, Is.StringContaining(typeof(DummyException).FullName));
        }

        [Test]
        public void message_and_stack_trace_should_be_rendered()
        {
            string renderedText = null;
            try
            {
                ThrowException(new DummyException("<message>"));
            }
            catch (Exception ex)
            {
                renderedText = RenderException(ex);
            }

            Assert.That(renderedText, Is.StringContaining("<message>") & Is.StringContaining("ThrowException"));
        }

        [Test]
        public void inner_exceptions_are_also_rendered()
        {
            string renderedText = null;
            try
            {
                ThrowException(new DummyException("<message1>", new DummyException("<exception2>")));
            }
            catch (DummyException ex)
            {
                renderedText = RenderException(ex);
            }

            Assert.That(renderedText, Is.StringContaining("<message1>") & Is.StringContaining("<exception2>"));
        }

        [Test]
        public void RendererMap_should_be_used_to_render_inner_exceptions()
        {
            DummyException innerException = new DummyException("<dummy-message>");
            FakeRenderer fakeRenderer = new FakeRenderer();
            fakeRenderer.TextToRender = "<fake-text>";

            map.Put(typeof(DummyException), fakeRenderer);

            string renderedText = null;
            try
            {
                ThrowException(new Exception("<message>", innerException));
            }
            catch (Exception ex)
            {
                renderedText = RenderException(ex);
            }

            Assert.That(renderedText, Is.StringContaining("<message>") &
                                      Is.StringContaining("<fake-text>") &
                                     !Is.StringContaining("<dummy-message>"));
        }

#if !NET_1_0 && !NET_1_1 && !MONO_1_0 && !NET_CF
        [Test]
        public void Data_context_should_be_rendered()
        {
            Exception ex = new Exception("<message>");
            ex.Data.Add("foo", "bar");
            ex.Data.Add("oscar", new System.Text.RegularExpressions.Regex("thegrouch"));

            FakeRenderer fakeRenderer = new FakeRenderer();
            fakeRenderer.TextToRender = "snuffleufagus";
            map.Put(typeof(Regex), fakeRenderer);

            string renderedText = RenderException(ex);
            Assert.That(renderedText, Is.StringContaining("foo") &
                                      Is.StringContaining("bar") &
                                      Is.StringContaining("snuffleufagus") &
                                     !Is.StringContaining("thegrouch"));
        }
#endif

        protected virtual void ThrowException(Exception ex)
        {
            throw ex;
        }

        private string RenderException(Exception ex)
        {
            StringWriter writer = new StringWriter();
            renderer.RenderObject(map, ex, writer);
            return writer.ToString();
        }

        private class DummyException : Exception
        {
            public DummyException() : base() { }
            public DummyException(string message) : base(message) { }
            public DummyException(string message, Exception innerException) : base(message, innerException) { }
        }

        private class FakeRenderer : IObjectRenderer
        {
            public object LastRenderedObject;
            public string TextToRender;

            #region IObjectRenderer Members

            public void RenderObject(RendererMap rendererMap, object obj, TextWriter writer)
            {
                if (TextToRender != null)
                    writer.Write(TextToRender);
                LastRenderedObject = obj;
            }

            #endregion
        }
    }
}
