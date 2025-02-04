﻿using System.IO;
using LMaML.Infrastructure.Audio;

namespace LMaML.Infrastructure.Util
{
    /// <summary>
    /// ID3FileBuilder
    /// </summary>
    public class ID3FileBuilder : IInfoBuilder<ID3File>
    {
        /// <summary>
        /// Builds the specified info.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="valid">if set to <c>true</c> [valid].</param>
        /// <returns></returns>
        public ID3File Build(FileInfo info, out bool valid)
        {
            ID3File ret;
            try
            {
                ret = new ID3File(info.FullName);
            }
            catch (PathTooLongException)
            {
                valid = false;
                return null;
            }
            valid = ret.IsValid;
            return ret;
        }
    }
}
