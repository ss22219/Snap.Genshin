﻿using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace DGP.Genshin.Helper
{
    /// <summary>
    /// Snap Genshin 文件路径解析上下文
    /// 不能用于处理其他位置的文件
    /// </summary>
    public static class PathContext
    {
        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FileExists(string file)
        {
            return File.Exists(Locate(file));
        }

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FolderExists(string folder)
        {
            return Directory.Exists(Locate(folder));
        }

        /// <summary>
        /// 定位根目录中的文件或文件夹
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="fileOrFolder"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Locate(string fileOrFolder)
        {
            return Path.GetFullPath(fileOrFolder, AppContext.BaseDirectory);
        }

        /// <summary>
        /// 定位根目录下子文件夹中的文件
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Locate(string folder, string file)
        {
            return Path.GetFullPath(Path.Combine(folder, file), AppContext.BaseDirectory);
        }

        /// <summary>
        /// 将文件移动到指定的子目录
        /// </summary>
        /// <param name="file"></param>
        /// <param name="folder"></param>
        /// <param name="overwrite"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool MoveToFolderOrIgnore(string file, string folder, bool overwrite = true)
        {
            string target = Locate(folder, file);
            file = Locate(file);

            if (File.Exists(file))
            {
                File.Move(file, target, overwrite);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 创建文件，若已存在文件，则不会创建
        /// </summary>
        /// <param name="file"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateFileOrIgnore(string file)
        {
            file = Locate(file);
            if (!File.Exists(file))
            {
                File.Create(file).Dispose();
            }
        }

        /// <summary>
        /// 创建文件夹，若已存在文件，则不会创建
        /// </summary>
        /// <param name="file"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateFolderOrIgnore(string folder)
        {
            folder = Locate(folder);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }

        /// <summary>
        /// 尝试删除文件夹
        /// </summary>
        /// <param name="file"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DeleteFolderOrIgnore(string folder)
        {
            folder = Locate(folder);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }
    }
}
