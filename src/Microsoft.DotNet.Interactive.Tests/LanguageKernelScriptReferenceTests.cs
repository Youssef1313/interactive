// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Tests;

[TestClass]
#pragma warning disable 8509
public class LanguageKernelScriptReferenceTests : LanguageKernelTestBase
{
    public LanguageKernelScriptReferenceTests(TestContext output) : base(output)
    {
    }

    [TestMethod]
    [DataRow(Language.CSharp)]
    [DataRow(Language.FSharp)]
    public async Task it_can_load_script_files_using_load_directive_with_relative_path(Language language)
    {
        var kernel = CreateKernel(language);

        var code = language switch
        {
            Language.CSharp => "#load \"RelativeLoadingSample.csx\"",
            Language.FSharp => "#load \"RelativeLoadingSample.fsx\""
        };

        var command = new SubmitCode(code);
        await kernel.SendAsync(command);

        KernelEvents.Should().NotContainErrors();

        KernelEvents.Should()
            .ContainSingle<DisplayedValueProduced>(e => e.FormattedValues.Any(v => v.Value.Contains("hello!")));
    }

    [TestMethod]
    [DataRow(Language.CSharp)]
    // [DataRow(Language.FSharp)] Not supported in F#
    public async Task it_can_load_script_files_using_load_directive_with_relative_path_after_user_code_changes_current_directory(Language language)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        DisposeAfterTest(() => Directory.SetCurrentDirectory(currentDirectory));

        var kernel = CreateKernel(language);

        //Even when a user changes the current directory, loading from a relative path is not affected.
        await kernel.SendAsync(new SubmitCode("System.IO.Directory.SetCurrentDirectory(\"..\")"));

        var code = language switch
        {
            Language.CSharp => $"#load \"{Path.Combine(currentDirectory, "RelativeLoadingSample.csx")}\"",
            Language.FSharp => $"#load \"{Path.Combine(currentDirectory, "RelativeLoadingSample.fsx")}\""
        };

        var command = new SubmitCode(code);
        await kernel.SendAsync(command);

        KernelEvents.Should().NotContainErrors();

        KernelEvents.Should()
            .ContainSingle<DisplayedValueProduced>(e => e.FormattedValues.Any(v => v.Value.Contains("hello!")));
    }
}