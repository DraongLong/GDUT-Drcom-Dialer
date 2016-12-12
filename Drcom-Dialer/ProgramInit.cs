﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Drcom_Dialer
{
    internal static class ProgramInit
    {
        private static Mutex singleInstanceWatcher;
        private static bool createdNew;

        [STAThread]
        private static void Main(string[] args)
        {
            //初始化Log
            Model.Utils.Log4Net.SetConfig();

            //Model.Utils.NetSharing ns = new Model.Utils.NetSharing();
            //if (ns.Start())
            //    Console.WriteLine("233");
            //else
            //    Console.WriteLine("555");

            //解析启动参数
            //不把更新放在这个之前也是因为考虑到可能使用带参启动的问题
            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case Model.Utils.VPNFixer.StartupArgs:
                        //初始化配置
                        Model.DialerConfig.Init();
                        Model.Utils.VPNFixer.AddRouteRule();
                        return;
                    default:
                        Model.Utils.Log4Net.WriteLog("未知的启动参数: " + args);
                        break;
                }
            }

            //防止多启
            Singleton();

            //更新
            if (Update())
            {
                return;
            }

            //初始化配置
            Model.DialerConfig.Init();

            //修复VPN
            if (Model.DialerConfig.isFixVPN)
            {
                Model.Utils.VPNFixer.Elevate();
            }

            //初始化必要组件
            Model.PPPoE.Init();
            Model.Dial.Init();
            Model.Utils.GDUT_Drcom.Load();
            Model.Utils.DialerUpdater.LaterCheckUpdate();

            //初始化界面
            App app = new App();
            app.InitializeComponent();
            app.Run();
        }

        private static void Singleton()
        {
            int count = Process.GetProcessesByName(Model.Utils.DialerUpdater.NoExtName).Length;
            if (count > 1)
            {
                MessageBox.Show("程序已经运行!", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(-1);
            }
            //string mutexName = Properties.Resources.ProgramTitle + "Mutex";
            //singleInstanceWatcher = new Mutex(false, mutexName, out createdNew);
            //if (!createdNew)
            //{
            //    MessageBox.Show("程序已经运行!", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            //    Environment.Exit(-1);
            //}
        }

        private static void WaitProcess(string name)
        {
            foreach (var ps in Process.GetProcessesByName(name))
            {
                if (!ps.WaitForExit(3000))
                {
                    ps.Kill();
                }
            }
        }

        /// <summary>
        /// 更新程序文件
        /// </summary>
        /// <returns>是否需要重启</returns>
        private static bool Update()
        {
            string name = AppDomain.CurrentDomain.FriendlyName;
            if (name.EndsWith(".exe"))
                name = name.Substring(0, name.Length - 4);
            int idx = name.LastIndexOf('.');
            if (idx == -1) // .exe
            {
                // Clean up
                if (File.Exists(Model.Utils.DialerUpdater.OldName + ".exe"))
                {
                    //MessageBox.Show("Delete " + Model.Utils.Updater.OldName + ".exe");
                    WaitProcess(Model.Utils.DialerUpdater.OldName);
                    File.Delete(Model.Utils.DialerUpdater.OldName + ".exe");
                }

                if (File.Exists(Model.Utils.DialerUpdater.NewName))
                {
                    Model.Utils.Log4Net.WriteLog("重命名 .new 到 .new.exe");
                    File.Move(Model.Utils.DialerUpdater.NewName, Model.Utils.DialerUpdater.NewName + ".exe");
                    Process.Start(Model.Utils.DialerUpdater.NewName + ".exe"); //是否有可能导致一个没关一个又开了呢？
                    return true;
                }
                else
                {
                    return false; // 正常流程
                }
            }
            else
            {
                string suff = name.Substring(idx);
                if (suff == Model.Utils.DialerUpdater.NewSuffix) // .new.exe
                {
                    if (File.Exists(Model.Utils.DialerUpdater.NoExtName + ".exe"))
                    {
                        Model.Utils.Log4Net.WriteLog("重命名 .exe 到 .old.exe");
                        WaitProcess(Model.Utils.DialerUpdater.NoExtName);
                        File.Move(Model.Utils.DialerUpdater.NoExtName + ".exe", Model.Utils.DialerUpdater.OldName + ".exe");
                        Process.Start(Model.Utils.DialerUpdater.OldName + ".exe");
                        return true;
                    }
                    else
                    {
                        Model.Utils.Log4Net.WriteLog("未找到 .exe，尝试直接使用 .new.exe");
                        File.Copy(Model.Utils.DialerUpdater.NewName + ".exe", Model.Utils.DialerUpdater.NoExtName + ".exe");
                        Process.Start(Model.Utils.DialerUpdater.NoExtName + ".exe");
                        return true;
                    }
                }
                else if (suff == Model.Utils.DialerUpdater.OldSuffix) // .old.exe
                {
                    if (File.Exists(Model.Utils.DialerUpdater.NewName + ".exe"))
                    {
                        Model.Utils.Log4Net.WriteLog("重命名 .new.exe to .exe");
                        WaitProcess(Model.Utils.DialerUpdater.NewName);
                        File.Move(Model.Utils.DialerUpdater.NewName + ".exe", Model.Utils.DialerUpdater.NoExtName + ".exe");
                        Process.Start(Model.Utils.DialerUpdater.NoExtName + ".exe");
                        return true;
                    }
                    else
                    {
                        Model.Utils.Log4Net.WriteLog("未找到 .new.exe，尝试还原");
                        File.Copy(Model.Utils.DialerUpdater.OldName + ".exe", Model.Utils.DialerUpdater.NoExtName + ".exe");
                        Process.Start(Model.Utils.DialerUpdater.NoExtName + ".exe");
                        return true;
                    }
                }
                else
                {
                    Model.Utils.Log4Net.WriteLog("无法识别的文件后缀");
                    // 正常流程
                    return false;
                }
            }
        }
    }
}
