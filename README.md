# Rixian.Drive.Common

***Supporting code for Rixian Drive components***

[![NuGet package](https://img.shields.io/nuget/v/Rixian.Drive.Common.svg)](https://nuget.org/packages/Rixian.Drive.Common)
[![Build Status](https://dev.azure.com/rixian/Cloud%20Platform/_apis/build/status/rixian.drive-common?branchName=master)](https://dev.azure.com/rixian/Cloud%20Platform/_build/latest?definitionId=85&branchName=master)
[![codecov](https://codecov.io/gh/rixian/drive-common/branch/master/graph/badge.svg)](https://codecov.io/gh/rixian/drive-common)

## Overview

* `CloudPath` class used to represent file paths agnostic of existing devices.

Install the latest NuGet package from: https://www.nuget.org/packages/Rixian.Drive.Common

## Cloud Path

`CloudPath` is an immutable class intended to represent a path and it's various components. You can find the specification here: [CloudPath Spec](docs/cloudpath_spec.md)

### Examples

To get an idea of what CloudPath is and how it looks here are some examples to start off:

* `C:/images/foo.png`
* `//shared/images/foo.png`
* `C:/images/foo.png:metadata`
* `/foo/../images/foo.png`

There are four components to a cloud path:

* Label - The name given to the partition or share.
* Path - The path itself.
* Stream - The name of a specific stream in the file.
* Type - The type of path: Partition, Share, or None

Lets use the examples and show how they are interpreted:

| Full Path                          | Label   | Path              | Stream     | Type        |
|------------------------------------|---------|-------------------|------------|-------------|
| `C:/images/foo.png`                | `C`     | `/images/foo.png` |            | `partition` |
| `//share:/images/foo.png`          | `share` | `/images/foo.png` |            | `share`     |
| `C:/images/foo.png:metadata`       | `C`     | `/images/foo.png` | `metadata` | `partition` |
| `/foo/../images/foo.png`           |         | `/images/foo.png` |            | `none`      |

More info to come soon...
