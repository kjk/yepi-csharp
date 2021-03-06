namespace Yepi
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Remoting;
    using System.Runtime.Remoting.Channels;
    using System.Runtime.Remoting.Channels.Ipc;
    using System.Runtime.Serialization.Formatters;
    using System.Threading;
    using System.Windows;
    using System.Windows.Threading;
    using System.Security;
    using System.Runtime.InteropServices;
    using System.ComponentModel;

    [SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {
        [DllImport("shell32.dll", EntryPoint = "CommandLineToArgvW", CharSet = CharSet.Unicode)]
        private static extern IntPtr _CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string cmdLine, out int numArgs);


        [DllImport("kernel32.dll", EntryPoint = "LocalFree", SetLastError = true)]
        private static extern IntPtr _LocalFree(IntPtr hMem);

        public static string[] CommandLineToArgvW(string cmdLine)
        {
            IntPtr argv = IntPtr.Zero;
            try
            {
                int numArgs = 0;

                argv = _CommandLineToArgvW(cmdLine, out numArgs);
                if (argv == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }
                var result = new string[numArgs];

                for (int i = 0; i < numArgs; i++)
                {
                    IntPtr currArg = Marshal.ReadIntPtr(argv, i * Marshal.SizeOf(typeof(IntPtr)));
                    result[i] = Marshal.PtrToStringUni(currArg);
                }

                return result;
            }
            finally
            {

                IntPtr p = _LocalFree(argv);
            }
        }
    } 

    public interface ISingleInstanceApp 
    { 
         bool SignalExternalCommandLineArgs(IList<string> args); 
    } 

    // Note: this class should be used with some caution, because it does no
    // security checking. For example, if one instance of an app that uses this class
    // is running as Administrator, any other instance, even if it is not
    // running as Administrator, can activate it with command line arguments.
    // For most apps, this will not be much of an issue.
    public static class SingleInstance<TApplication>  
                where   TApplication: Application ,  ISingleInstanceApp 
                                    
    {
        private const string Delimiter = ":";
        private const string ChannelNameSuffix = "SingeInstanceIPCChannel";
        private const string RemoteServiceName = "SingleInstanceApplicationService";
        private const string IpcProtocol = "ipc://";
        private static Mutex singleInstanceMutex;
        private static IpcServerChannel channel;

        // Checks if the instance of the application attempting to start is the first instance. 
        // If not, activates the first instance.
        // Returns True if this is the first instance of the application.
        public static bool InitializeAsFirstInstance(string uniqueName, bool notify = true)
        {
            var channelName = ChannelName(uniqueName);
            var appId = AppId(uniqueName);
            // Create mutex based on unique application Id to check if this is the first instance of the application. 
            bool firstInstance;
            singleInstanceMutex = new Mutex(true, appId, out firstInstance);
            if (firstInstance)
                CreateRemoteService(channelName);
            else if (notify)
            {
                var cmdArgs = GetCommandLineArgs(uniqueName);
                SignalFirstInstance(channelName, cmdArgs);
            }

            return firstInstance;
        }

        // Build unique application Id and the IPC channel name.
        private static string ChannelName(string uniqueName)
        {
            string appId = AppId(uniqueName);
            return String.Concat(appId, Delimiter, ChannelNameSuffix);
        }

        private static string AppId(string uniqueName)
        {
            return uniqueName + Environment.UserName;
        }

        public static void Cleanup()
        {
            if (singleInstanceMutex != null)
            {
                singleInstanceMutex.Close();
                singleInstanceMutex = null;
            }

            if (channel != null)
            {
                ChannelServices.UnregisterChannel(channel);
                channel = null;
            }
        }

        // Gets command line args - for ClickOnce deployed applications, command line
        // args may not be passed directly, they have to be retrieved.
        // Returns list of command line arg strings.
        public static IList<string> GetCommandLineArgs(string uniqueApplicationName)
        {
            string[] args = null;
            if (AppDomain.CurrentDomain.ActivationContext == null)
            {
                // The application was not clickonce deployed, get args from standard API's
                args = Environment.GetCommandLineArgs();
            }
            else
            {
                // The application was clickonce deployed
                // Clickonce deployed apps cannot recieve traditional commandline arguments
                // As a workaround commandline arguments can be written to a shared location before 
                // the app is launched and the app can obtain its commandline arguments from the 
                // shared location               
                string appFolderPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), uniqueApplicationName);

                string cmdLinePath = Path.Combine(appFolderPath, "cmdline.txt");
                if (File.Exists(cmdLinePath))
                {
                    try
                    {
                        using (TextReader reader = new StreamReader(cmdLinePath, System.Text.Encoding.Unicode))
                        {
                            args = NativeMethods.CommandLineToArgvW(reader.ReadToEnd());
                        }

                        File.Delete(cmdLinePath);
                    }
                    catch (IOException)
                    {
                    }
                }
            }

            if (args == null)
            {
                args = new string[] { };
            }

            return new List<string>(args);
        }
        private static void CreateRemoteService(string channelName)
        {
            BinaryServerFormatterSinkProvider serverProvider = new BinaryServerFormatterSinkProvider();
            serverProvider.TypeFilterLevel = TypeFilterLevel.Full;
            IDictionary props = new Dictionary<string, string>();

            props["name"] = channelName;
            props["portName"] = channelName;
            props["exclusiveAddressUse"] = "false";

            channel = new IpcServerChannel(props, serverProvider);
            ChannelServices.RegisterChannel(channel, true);
            IPCRemoteService remoteService = new IPCRemoteService();
            RemotingServices.Marshal(remoteService, RemoteServiceName);
        }

        // Creates a client channel and obtains a reference to the remoting service exposed by the server - 
        // in this case, the remoting service exposed by the first instance. Calls a function of the remoting service 
        // class to pass on command line arguments from the second instance to the first and cause it to activate itself.
        // channelName: Application's IPC channel name
        // args: command line arguments for the second instance, passed to the first instance to take appropriate action.
        public static void SignalFirstInstance(string uniqueName, IList<string> args)
        {
            string channelName = ChannelName(uniqueName);
            IpcClientChannel secondInstanceChannel = new IpcClientChannel();
            ChannelServices.RegisterChannel(secondInstanceChannel, true);

            string remotingServiceUrl = IpcProtocol + channelName + "/" + RemoteServiceName;

            // Obtain a reference to the remoting service exposed by the server i.e the first instance of the application
            IPCRemoteService firstInstanceRemoteServiceReference = (IPCRemoteService)RemotingServices.Connect(typeof(IPCRemoteService), remotingServiceUrl);

            // Check that the remote service exists, in some cases the first instance may not yet have created one, in which case
            // the second instance should just exit
            if (firstInstanceRemoteServiceReference != null)
            {
                // Invoke a method of the remote service exposed by the first instance passing on the command line
                // arguments and causing the first instance to activate itself
                firstInstanceRemoteServiceReference.InvokeFirstInstance(args);
            }
        }

        // Callback for activating first instance of the application.
        private static object ActivateFirstInstanceCallback(object arg)
        {
            // Get command line args to be passed to first instance
            IList<string> args = arg as IList<string>;
            ActivateFirstInstance(args);
            return null;
        }

        // Activates the first instance of the application with arguments from a second instance.
        private static void ActivateFirstInstance(IList<string> args)
        {
            // Set main window state and process command line args
            if (Application.Current == null)
            {
                return;
            }

            ((TApplication)Application.Current).SignalExternalCommandLineArgs(args);
        }

        // Remoting service class which is exposed by the server i.e the first instance and called by the second instance
        // to pass on the command line arguments to the first instance and cause it to activate itself.
        private class IPCRemoteService : MarshalByRefObject
        {
            // Activates the first instance of the application.
            public void InvokeFirstInstance(IList<string> args)
            {
                if (Application.Current != null)
                {
                    // Do an asynchronous call to ActivateFirstInstance function
                    Application.Current.Dispatcher.BeginInvoke(
                        DispatcherPriority.Normal, new DispatcherOperationCallback(SingleInstance<TApplication>.ActivateFirstInstanceCallback), args);
                }
            }

            // Remoting Object's ease expires after every 5 minutes by default. We need to override the InitializeLifetimeService class
            // to ensure that lease never expires.
            public override object InitializeLifetimeService()
            {
                return null;
            }
        }
    }
}