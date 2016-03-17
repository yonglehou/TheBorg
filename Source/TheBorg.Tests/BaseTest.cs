﻿// The MIT License(MIT)
// 
// Copyright(c) 2016 Rasmus Mikkelsen
// https://github.com/rasmus/TheBorg
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Moq;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Serilog;

namespace TheBorg.Tests
{
    public abstract class BaseTest
    {
        protected IFixture Fixture { get; private set; }

        protected CancellationToken C => CancellationToken.None;

        [SetUp]
        public void BaseTestSetUp()
        {
            Fixture = new Fixture();

            Fixture.Inject(CreateLogger());
        }

        protected virtual ILogger CreateLogger()
        {
            return new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .CreateLogger();
        }

        protected T A<T>()
        {
            return Fixture.Create<T>();
        }

        protected IReadOnlyCollection<T> Many<T>(int count = 3)
        {
            return Fixture.CreateMany<T>().ToList();
        }

        protected virtual Mock<T> Mock<T>()
            where T : class
        {
            var mock = new Mock<T>();
            Fixture.Inject(mock.Object);
            return mock;
        } 
    }
}