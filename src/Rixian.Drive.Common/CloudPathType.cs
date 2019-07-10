// Copyright (c) Rixian. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.

namespace Rixian.Drive.Common
{
    /// <summary>
    /// The type of cloud path.
    /// </summary>
    public enum CloudPathType
    {
        /// <summary>
        /// No type, or unknown.
        /// </summary>
        None = 0,

        /// <summary>
        /// A file share path, e.g. '//foo'
        /// </summary>
        Share,

        /// <summary>
        /// A partition path, e.g. 'foo:/'
        /// </summary>
        Partition,
    }
}
