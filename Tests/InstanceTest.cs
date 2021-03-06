using System;
using System.Collections.Generic;
using VulkanCore.Ext;
using VulkanCore.Khx;
using VulkanCore.Tests.Utilities;
using Xunit;
using Xunit.Abstractions;
using static VulkanCore.Constant;

namespace VulkanCore.Tests
{
    public unsafe class InstanceTest : HandleTestBase
    {
        [Fact]
        public void Constructor()
        {
            using (new Instance()) { }
            using (var instance = new Instance(allocator: CustomAllocator))
            {
                Assert.Equal(CustomAllocator, instance.Allocator);
            }
        }

        [Fact]
        public void ConstructorWithApplicationInfo()
        {
            var createInfo1 = new InstanceCreateInfo(new ApplicationInfo());
            var createInfo2 = new InstanceCreateInfo(new ApplicationInfo("app name", 1, "engine name", 2));
            using (new Instance(createInfo1)) { }
            using (new Instance(createInfo2)) { }
        }

        [Fact]
        public void ConstructorWithEnabledLayerAndExtension()
        {
            var createInfo = new InstanceCreateInfo(
                enabledLayerNames: new[] { InstanceLayer.LunarGStandardValidation },
                enabledExtensionNames: new[] { InstanceExtension.ExtDebugReport });

            using (new Instance(createInfo)) { }
        }

        [Fact]
        public void DisposeTwice()
        {
            var instance = new Instance();
            instance.Dispose();
            instance.Dispose();
        }

        [Fact]
        public void CreateDebugReportCallbackExt()
        {
            var createInfo = new InstanceCreateInfo(
                enabledLayerNames: new[] { InstanceLayer.LunarGStandardValidation },
                enabledExtensionNames: new[] { InstanceExtension.ExtDebugReport });

            using (var instance = new Instance(createInfo))
            {
                var callbackArgs = new List<DebugReportCallbackInfo>();
                int userData = 1;
                IntPtr userDataHandle = new IntPtr(&userData);
                var debugReportCallbackCreateInfo = new DebugReportCallbackCreateInfoExt(
                    DebugReportFlagsExt.All,
                    args =>
                    {
                        callbackArgs.Add(args);
                        return false;
                    },
                    userDataHandle);

                // Registering the callback should generate DEBUG messages.
                using (instance.CreateDebugReportCallbackExt(debugReportCallbackCreateInfo)) { }
                using (instance.CreateDebugReportCallbackExt(debugReportCallbackCreateInfo, CustomAllocator)) { }

                Assert.True(callbackArgs.Count > 0);
                Assert.Equal(1, *(int*)callbackArgs[0].UserData);
            }
        }

        [Fact]
        public void EnumeratePhysicalDevices()
        {
            PhysicalDevice[] physicalDevices = Instance.EnumeratePhysicalDevices();
            Assert.True(physicalDevices.Length > 0);
            Assert.Equal(Instance, physicalDevices[0].Parent);
        }

        [Fact]
        public void EnumeratePhysicalDeviceGroupsKhx()
        {
            if (!AvailableDeviceExtensions.Contains(InstanceExtension.KhxDeviceGroupCreation)) return;

            var createInfo = new InstanceCreateInfo(
                enabledExtensionNames: new[] { InstanceExtension.KhxDeviceGroupCreation });
            using (var instance = new Instance(createInfo))
            {
                instance.EnumeratePhysicalDeviceGroupsKhx();
            }
        }

        [Fact]
        public void GetProcAddrForExistingCommand()
        {
            IntPtr address = Instance.GetProcAddr("vkCreateDebugReportCallbackEXT");
            Assert.NotEqual(IntPtr.Zero, address);
        }

        [Fact]
        public void GetProcAddrForMissingCommand()
        {
            IntPtr address = Instance.GetProcAddr("does not exist");
            Assert.Equal(IntPtr.Zero, address);
        }

        [Fact]
        public void GetProcAddrForNull()
        {
            Assert.Throws<ArgumentNullException>(() => Instance.GetProcAddr(null));
        }

        [Fact]
        public void GetProcForExistingCommand()
        {
            var commandDelegate = Instance.GetProc<CreateDebugReportCallbackExtDelegate>("vkCreateDebugReportCallbackEXT");
            Assert.NotNull(commandDelegate);
        }

        [Fact]
        public void GetProcForMissingCommand()
        {
            Assert.Null(Instance.GetProc<EventHandler>("does not exist"));
        }

        [Fact]
        public void GetProcForNull()
        {
            Assert.Throws<ArgumentNullException>(() => Instance.GetProc<EventHandler>(null));
        }

        [Fact]
        public void EnumerateExtensionPropertiesForAllLayers()
        {
            ExtensionProperties[] properties = Instance.EnumerateExtensionProperties();
            Assert.True(properties.Length > 0);
        }

        [Fact]
        public void EnumerateExtensionPropertiesForSingleLayer()
        {
            ExtensionProperties[] properties = Instance.EnumerateExtensionProperties(
                Constant.InstanceLayer.LunarGStandardValidation);
            Assert.True(properties.Length > 0);

            ExtensionProperties firstProperty = properties[0];
            Assert.StartsWith(firstProperty.ExtensionName, properties[0].ToString());
        }

        [Fact]
        private void EnumerateLayerProperties()
        {
            LayerProperties[] properties = Instance.EnumerateLayerProperties();
            Assert.True(properties.Length > 0);

            LayerProperties firstProperty = properties[0];
            Assert.StartsWith(firstProperty.LayerName, properties[0].ToString());
        }

        [Fact]
        public void DebugReportMessageExt()
        {
            const string message = "message õäöü";
            const DebugReportObjectTypeExt objectType = DebugReportObjectTypeExt.DebugReportCallback;
            const long @object = long.MaxValue;
            var location = new IntPtr(int.MaxValue);
            const int messageCode = 1;
            const string layerPrefix = "prefix õäöü";

            bool visitedCallback = false;

            var instanceCreateInfo = new InstanceCreateInfo(
                enabledExtensionNames: new[] { InstanceExtension.ExtDebugReport });
            using (var instance = new Instance(instanceCreateInfo))
            {
                var debugReportCallbackCreateInfo = new DebugReportCallbackCreateInfoExt(
                    DebugReportFlagsExt.Error,
                    args =>
                    {
                        Assert.Equal(objectType, args.ObjectType);
                        Assert.Equal(@object, args.Object);
                        Assert.Equal(location, args.Location);
                        Assert.Equal(messageCode, args.MessageCode);
                        Assert.Equal(layerPrefix, args.LayerPrefix);
                        Assert.Equal(message, args.Message);
                        visitedCallback = true;
                        return false;
                    });
                using (instance.CreateDebugReportCallbackExt(debugReportCallbackCreateInfo))
                {
                    instance.DebugReportMessageExt(DebugReportFlagsExt.Error, message, objectType,
                        @object, location, messageCode, layerPrefix);
                }
            }

            Assert.True(visitedCallback);
        }

        public InstanceTest(DefaultHandles defaults, ITestOutputHelper output) : base(defaults, output) { }

        private delegate Result CreateDebugReportCallbackExtDelegate(IntPtr p1, IntPtr p2, IntPtr p3, IntPtr p4);
    }
}
