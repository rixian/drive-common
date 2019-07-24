// Copyright (c) Rixian. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Rixian.Drive.Common;
using Xunit;

public class CloudPathTests
{
    [Theory]
    [InlineData(@"\foo\bar.txt", "/foo/bar.txt", null)]
    [InlineData(@"   /foo/bar.txt   ", "/foo/bar.txt", null)]
    [InlineData(@"\foo\bar.txt:abcd", "/foo/bar.txt", "abcd")]
    [InlineData(@"   /foo/bar.txt:abcd   ", "/foo/bar.txt", "abcd")]
    [InlineData(@"C:\foo\bar.txt", "/foo/bar.txt", null)]
    [InlineData(@"C:\foo\bar.txt:abcd", "/foo/bar.txt", "abcd")]
    [InlineData(@"\\QQQ\foo\bar.txt", "/foo/bar.txt", null)]
    [InlineData(@"\\QQQ\foo\bar.txt:abcd", "/foo/bar.txt", "abcd")]
    public void ParsePath(string fullPath, string expectedPath, string expectedStream)
    {
        var pathInfo = new CloudPath(fullPath);
        pathInfo.Should().NotBeNull();
        pathInfo.Path.Should().Be(expectedPath);
        pathInfo.Stream.Should().Be(expectedStream);
    }

    [Theory]
    [InlineData(@"\foo\bar.txt", null, "/foo/bar.txt", null, "/foo/bar.txt")]
    [InlineData(@"   /foo/bar.txt   ", null, "/foo/bar.txt", null, "/foo/bar.txt")]
    [InlineData(@"\foo\bar.txt:abcd", null, "/foo/bar.txt", "abcd", "/foo/bar.txt:abcd")]
    [InlineData(@"   /foo/bar.txt:abcd   ", null, "/foo/bar.txt", "abcd", "/foo/bar.txt:abcd")]
    [InlineData(@"C:\foo\bar.txt", "C", "/foo/bar.txt", null, "C:/foo/bar.txt")]
    [InlineData(@"C:\foo\bar.txt:abcd", "C", "/foo/bar.txt", "abcd", "C:/foo/bar.txt:abcd")]
    [InlineData(@"\\QQQ\foo\bar.txt", "QQQ", "/foo/bar.txt", null, "//QQQ/foo/bar.txt")]
    [InlineData(@"\\QQQ\foo\bar.txt:abcd", "QQQ", "/foo/bar.txt", "abcd", "//QQQ/foo/bar.txt:abcd")]
    public void ImplicitParsePath(string fullPath, string expectedLabel, string expectedPath, string expectedStream, string expectedFull)
    {
        CloudPath pathInfo = fullPath;
        pathInfo.Should().NotBeNull();
        pathInfo.Label.Should().Be(expectedLabel);
        pathInfo.Path.Should().Be(expectedPath);
        pathInfo.Stream.Should().Be(expectedStream);

        string stringPath = pathInfo;
        stringPath.Should().NotBeNull();
        stringPath.Should().Be(expectedFull);
    }

    [Theory]
    [InlineData(@"\foo\bar.txt", "")]
    [InlineData(@"   /foo/bar.txt   ", "")]
    [InlineData(@"\foo\bar.txt:abcd", "")]
    [InlineData(@"   /foo/bar.txt:abcd   ", "")]
    [InlineData(@"C:\foo\bar.txt", "C")]
    [InlineData(@"C:\foo\bar.txt:abcd", "C")]
    [InlineData(@"\\QQQ\foo\bar.txt", "QQQ")]
    [InlineData(@"\\QQQ\foo\bar.txt:abcd", "QQQ")]
    [InlineData(@"C:/foo/bar.txt", "C")]
    [InlineData(@"C:/", "C")]
    [InlineData(@"C:", "C")]
    [InlineData(@"//share/foo/bar.txt", "share")]
    [InlineData(@"//share/", "share")]
    [InlineData(@"//share", "share")]
    public void GetPathLabel(string fullPath, string expectedLabel)
    {
        var label = CloudPath.GetPathLabel(fullPath);
        label.Should().Be(expectedLabel);
    }

    [Theory]
    [InlineData(@"C:", true)]
    [InlineData(@"C:/", true)]
    [InlineData(@"C:/foo", false)]
    [InlineData(@"C:/foo/", true)]
    [InlineData(@"C:/foo/bar.txt", false)]
    [InlineData(@"//share", true)]
    [InlineData(@"//share/", true)]
    [InlineData(@"//share/foo", false)]
    [InlineData(@"//share/foo/", true)]
    [InlineData(@"//share/foo/bar.txt", false)]
    [InlineData(@"/", true)]
    [InlineData(@"/foo", false)]
    [InlineData(@"/foo/", true)]
    [InlineData(@"/foo/bar.txt", false)]
    public void IsDirectoryPath(string fullPath, bool expectValue)
    {
        CloudPath path = fullPath;
        path.IsDirectoryPath.Should().Be(expectValue);
    }

    [Theory]
    [InlineData(@"C:", false)]
    [InlineData(@"C:/", false)]
    [InlineData(@"C:/foo", true)]
    [InlineData(@"C:/foo/", false)]
    [InlineData(@"C:/foo/bar.txt", true)]
    [InlineData(@"//share", false)]
    [InlineData(@"//share/", false)]
    [InlineData(@"//share/foo", true)]
    [InlineData(@"//share/foo/", false)]
    [InlineData(@"//share/foo/bar.txt", true)]
    [InlineData(@"/", false)]
    [InlineData(@"/foo", true)]
    [InlineData(@"/foo/", false)]
    [InlineData(@"/foo/bar.txt", true)]
    public void IsFilePath(string fullPath, bool expectValue)
    {
        CloudPath path = fullPath;
        path.IsFilePath.Should().Be(expectValue);
    }

    [Theory]
    [InlineData(@"C:")]
    [InlineData(@"C:/")]
    [InlineData(@"C:/foo")]
    [InlineData(@"C:/foo/")]
    [InlineData(@"C:/foo/bar.txt")]
    [InlineData(@"//share")]
    [InlineData(@"//share/")]
    [InlineData(@"//share/foo")]
    [InlineData(@"//share/foo/")]
    [InlineData(@"//share/foo/bar.txt")]
    [InlineData(@"/")]
    [InlineData(@"/foo")]
    [InlineData(@"/foo/")]
    [InlineData(@"/foo/bar.txt")]
    public void FormatAsDirectory(string fullPath)
    {
        CloudPath path = fullPath;
        CloudPath directoryPath = path.FormatAsDirectory();
        directoryPath.IsDirectoryPath.Should().Be(true);
    }

    [Theory]
    [InlineData(@"C:/foo:abcd")]
    [InlineData(@"C:/foo/bar.txt:abcd")]
    [InlineData(@"//share/foo:abcd")]
    [InlineData(@"//share/foo/bar.txt:abcd")]
    [InlineData(@"/foo:abcd")]
    [InlineData(@"/foo/bar.txt:abcd")]
    public void FormatAsDirectory_Stream_Exception(string fullPath)
    {
        CloudPath path = fullPath;
        Assert.Throws<InvalidOperationException>(() => path.FormatAsDirectory());
    }

    [Theory]
    [InlineData(@"C:/foo")]
    [InlineData(@"C:/foo/")]
    [InlineData(@"C:/foo/bar.txt")]
    [InlineData(@"//share/foo")]
    [InlineData(@"//share/foo/")]
    [InlineData(@"//share/foo/bar.txt")]
    [InlineData(@"/foo")]
    [InlineData(@"/foo/")]
    [InlineData(@"/foo/bar.txt")]
    public void FormatAsFile(string fullPath)
    {
        CloudPath path = fullPath;
        CloudPath directoryPath = path.FormatAsFile();
        directoryPath.IsDirectoryPath.Should().Be(false);
    }

    [Theory]
    [InlineData(@"C:")]
    [InlineData(@"C:/")]
    [InlineData(@"//share")]
    [InlineData(@"//share/")]
    [InlineData(@"/")]
    public void FormatAsFile_RootPath_Exception(string fullPath)
    {
        CloudPath path = fullPath;
        Assert.Throws<InvalidOperationException>(() => path.FormatAsFile());
    }

    [Theory]
    [InlineData(@"\foo\bar.txt", "/foo/bar.txt")]
    [InlineData(@"   /foo/bar.txt   ", "/foo/bar.txt")]
    [InlineData(@"\foo\bar.txt:abcd", "/foo/bar.txt")]
    [InlineData(@"   /foo/bar.txt:abcd   ", "/foo/bar.txt")]
    [InlineData(@"C:\foo\bar.txt", "C:/foo/bar.txt")]
    [InlineData(@"C:\foo\bar.txt:abcd", "C:/foo/bar.txt")]
    [InlineData(@"\\QQQ\foo\bar.txt", "//QQQ/foo/bar.txt")]
    [InlineData(@"\\QQQ\foo\bar.txt:abcd", "//QQQ/foo/bar.txt")]
    public void ToString_NoStream(string fullPath, string expectedPath)
    {
        CloudPath path = fullPath;
        var cleanPath = path.ToString(false);
        cleanPath.Should().Be(expectedPath);
    }

    [Theory]
    [InlineData(@"\foo\bar.txt", "/foo/bar.txt")]
    [InlineData(@"   /foo/bar.txt   ", "/foo/bar.txt")]
    [InlineData(@"\foo\bar.txt:abcd", "/foo/bar.txt:abcd")]
    [InlineData(@"   /foo/bar.txt:abcd   ", "/foo/bar.txt:abcd")]
    [InlineData(@"C:\foo\bar.txt", "C:/foo/bar.txt")]
    [InlineData(@"C:\foo\bar.txt:abcd", "C:/foo/bar.txt:abcd")]
    [InlineData(@"\\QQQ\foo\bar.txt", "//QQQ/foo/bar.txt")]
    [InlineData(@"\\QQQ\foo\bar.txt:abcd", "//QQQ/foo/bar.txt:abcd")]
    public void ToString_WithStream(string fullPath, string expectedPath)
    {
        CloudPath path = fullPath;
        var cleanPath = path.ToString(true);
        cleanPath.Should().Be(expectedPath);

        var cleanPath2 = path.ToString();
        cleanPath2.Should().Be(expectedPath);
    }

    [Theory]
    [InlineData(@"/foo/bar.txt", "/foo/bar.txt")]
    [InlineData(@"/foo/../bar.txt", "/bar.txt")]
    [InlineData(@"/foo/./bar.txt", "/foo/bar.txt")]
    [InlineData(@"/foo/./././././bar.txt", "/foo/bar.txt")]
    [InlineData(@"/foo/", "/foo/")]
    [InlineData(@"/foo/../bar/", "/bar/")]
    [InlineData(@"/foo/./bar/", "/foo/bar/")]
    [InlineData(@"/foo/./././././bar/", "/foo/bar/")]
    public void NormalizePath(string fullPath, string expectedPath)
    {
        var pathInfo = CloudPath.NormalizePath(fullPath);
        pathInfo.Should().NotBeNull();
        pathInfo.Should().Be(expectedPath);
    }

    [Theory]
    [InlineData(@"/foo/bar.txt", "/foo/bar.txt")]
    [InlineData(@"/foo/../bar.txt", "/bar.txt")]
    [InlineData(@"/foo/./bar.txt", "/foo/bar.txt")]
    [InlineData(@"/foo/./././././bar.txt", "/foo/bar.txt")]
    [InlineData(@"/foo/", "/foo/")]
    [InlineData(@"/foo/../bar/", "/bar/")]
    [InlineData(@"/foo/./bar/", "/foo/bar/")]
    [InlineData(@"/foo/./././././bar/", "/foo/bar/")]
    public void Implicit_NormalizePath(string fullPath, string expectedPath)
    {
        CloudPath pathInfo = fullPath;
        pathInfo.Should().NotBeNull();
        pathInfo.ToString().Should().Be(expectedPath);
    }

    [Theory]
    [InlineData(@"/foo/bar.txt", false)]
    [InlineData(@"/", true)]
    public void IsRoot(string fullPath, bool isRoot)
    {
        var pathInfo = fullPath;
        pathInfo.Should().NotBeNull();
        CloudPath.IsRoot(pathInfo).Should().Be(isRoot);
    }

    [Theory]
    [InlineData("/foo/bar.txt", "/foo/")]
    [InlineData("/bar.txt", "/")]
    [InlineData("/foo/", "/")]
    [InlineData("/A Name With Spaces/", "/")]
    [InlineData("/A/B/C/D/E/F/G/H/I/J/", "/A/B/C/D/E/F/G/H/I/")]
    public void GetParent(string fullPath, string expectedPath)
    {
        var pathInfo = CloudPath.GetParent(fullPath);
        pathInfo.Should().NotBeNull();
        pathInfo.Should().Be(expectedPath);
    }

    [Theory]
    [InlineData("/")]
    public void GetParent_Root(string fullPath)
    {
        var pathInfo = fullPath;
        Action act = () => CloudPath.GetParent(pathInfo);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(@"/foo/bar.txt", "test", "/foo/bar.txt/test")]
    [InlineData(@"/", "test", "/test")]
    public void AppendPath(string fullPath, string relativePath, string expectedPath)
    {
        var pathInfo = CloudPath.Combine(fullPath, relativePath);
        pathInfo.Should().NotBeNull();
        pathInfo.Should().Be(expectedPath);
    }

    [Theory]
    [InlineData(@"/foo/bar.txt", ".cmd", "/foo/bar.cmd")]
    [InlineData(@"/foo/bar.txt", "cmd", "/foo/bar.cmd")]
    [InlineData(@"/foo/bar.txt", "", "/foo/bar")]
    [InlineData(@"/foo/bar.txt", null, "/foo/bar")]
    [InlineData(@"/foo/bar", ".cmd", "/foo/bar.cmd")]
    [InlineData(@"/foo/bar", "cmd", "/foo/bar.cmd")]
    [InlineData(@"/foo/bar", "", "/foo/bar")]
    [InlineData(@"/foo/bar", null, "/foo/bar")]
    public void ChangeExtension(string fullPath, string extension, string expectedPath)
    {
        var path = CloudPath.ChangeExtension(fullPath, extension);
        path.Should().Be(expectedPath);
    }

    [Theory]
    [InlineData(@"/foo/", "/foo")]
    [InlineData(@"/foo", null)]
    [InlineData(@"/foo/bar.txt", "/foo")]
    [InlineData(@"C:/foo/bar.txt", "C:/foo")]
    [InlineData(@"//share/foo/bar.txt", "//share/foo")]
    public void GetDirectoryName(string fullPath, string expectedPath)
    {
        var path = CloudPath.GetDirectoryName(fullPath);
        path.Should().Be(expectedPath);
    }

    [Theory]
    [InlineData(@"/foo/bar.txt", ".txt")]
    [InlineData(@"/foo/bar.abcdefghijklmnop", ".abcdefghijklmnop")]
    [InlineData(@"/foo/bar.test.txt", ".txt")]
    [InlineData(@"/foo.bar/baz.test.txt", ".txt")]
    public void GetExtension(string fullPath, string expectedExtension)
    {
        var extension = CloudPath.GetExtension(fullPath);
        extension.Should().Be(expectedExtension);
    }

    [Theory]
    [InlineData(@"/foo/bar.txt", "bar.txt")]
    [InlineData(@"/foo/bar.abcdefghijklmnop", "bar.abcdefghijklmnop")]
    [InlineData(@"/foo/bar", "bar")]
    [InlineData(@"/foo/", "")]
    [InlineData(@"/foo", "foo")]
    public void GetFileName(string fullPath, string expectedFileName)
    {
        var extension = CloudPath.GetFileName(fullPath);
        extension.Should().Be(expectedFileName);
    }

    [Theory]
    [InlineData(@"C:/foo/bar.txt", "C:")]
    [InlineData(@"C:/", "C:")]
    [InlineData(@"C:", "C:")]
    [InlineData(@"//share/foo/bar.txt", "//share")]
    [InlineData(@"//share/", "//share")]
    [InlineData(@"//share", "//share")]
    public void GetPathRoot(string fullPath, string expectedRoot)
    {
        var root = CloudPath.GetPathRoot(fullPath);
        root.Should().Be(expectedRoot);
    }

    [Theory]
    [InlineData(@"/foo/bar.txt", true)]
    [InlineData(@"/foo/bar.abcdefghijklmnop", true)]
    [InlineData(@"/foo/bar.test.txt", true)]
    [InlineData(@"/foo.bar/baz.test.txt", true)]
    public void HasExtension(string fullPath, bool expectedResult)
    {
        var extension = CloudPath.HasExtension(fullPath);
        extension.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(@"/foo/bar.txt")]
    [InlineData(@"C:/foo/bar.txt")]
    [InlineData(@"/foo/bar.txt:baz")]
    [InlineData(@"C:/foo/bar.txt:baz")]
    public void Equality(string fullPath)
    {
        CloudPath path = fullPath;
        path.Should().Be((CloudPath)fullPath);

        path.Equals((string)fullPath).Should().BeTrue();
        path.Equals((CloudPath)fullPath).Should().BeTrue();
        path.Equals((object)fullPath).Should().BeTrue();
    }
}
