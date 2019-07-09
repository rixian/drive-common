// Copyright (c) Rixian. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENSE file in the project root for full license information.

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
    public void ParsePath(string fullPath, string expectedPath, string expectedStream)
    {
        var pathInfo = new CloudPath(fullPath);
        pathInfo.Should().NotBeNull();
        pathInfo.Path.Should().Be(expectedPath);
        pathInfo.Stream.Should().Be(expectedStream);
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
    public void GetPathRoot(string fullPath, string expectedLabel)
    {
        var root = CloudPath.GetPathRoot(fullPath);
        root.Should().Be(expectedLabel);
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
}
