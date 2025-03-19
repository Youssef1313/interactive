// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

[TestClass]
public class ProjectFileTests
{
    [TestMethod]
    public void file_can_only_be_with_relative_paths()
    {
        Action buildProjectFile = () =>
        {
            var pf = new ProjectFile(@"C:\Program.cs", "");
        };

        buildProjectFile.Should().Throw<ArgumentException>().WithMessage(@"Path cannot be absolute: C:\Program.cs");
    }
}