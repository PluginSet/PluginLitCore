using System;
using System.Collections.Generic;

namespace PluginLit.Core.Editor
{
    // 编译相关代码需要继承该类方便管理
    [AttributeUsage(AttributeTargets.Class)]
    public class BuildToolsAttribute : Attribute
    {
    }
    
    // 准备构建前按序执行，需要准备资源，接受BuildProccessorContext
    [AttributeUsage(AttributeTargets.Method)]
    public class PrepareAssetsBeforeBuildAttribute: OrderCallBack
    {
        public PrepareAssetsBeforeBuildAttribute()
            : base(0)
        {
            
        }

        public PrepareAssetsBeforeBuildAttribute(int order)
            : base(order)
        {
            
        }
    }
    
    
    // 构建完成后回调，接受BuildProccessorContext
    [AttributeUsage(AttributeTargets.Method)]
    public class BuildProjectCompletedAttribute : OrderCallBack
    {
        public BuildProjectCompletedAttribute()
            : base(0)
        {
            
        }

        public BuildProjectCompletedAttribute(int order)
            : base(order)
        {
            
        }
    }
    
    // 安卓项目设定工程时调用该接口
    // 该接口有序调用，接收BuildProccessorContext, AndroidProjectManager
    [AttributeUsage(AttributeTargets.Method)]
    public class AndroidProjectModifyAttribute : OrderCallBack
    {
        public AndroidProjectModifyAttribute()
            : base(0)
        {
        }

        public AndroidProjectModifyAttribute(int order)
            : base(order)
        {
        }
    }
    

    // 苹果项目设定XCode工程时调用该接口
    // 该接口有序调用，接收BuildProccessorContext, PBXProject
    [AttributeUsage(AttributeTargets.Method)]
    public class iOSXCodeProjectModifyAttribute : OrderCallBack
    {
        public iOSXCodeProjectModifyAttribute()
            : base(0)
        {
        }

        public iOSXCodeProjectModifyAttribute(int order)
            : base(order)
        {
        }
    }

    // WebGL项目设定项目导出目录时调用该接口
    // 该接口有序调用，接收BuildProccessorContext, string
    [AttributeUsage(AttributeTargets.Method)]
    public class WebGLProjectModifyAttribute : OrderCallBack
    {
        public WebGLProjectModifyAttribute()
            : base(0)
        {
        }

        public WebGLProjectModifyAttribute(int order)
            : base(order)
        {
        }
    }
    
    // 重置编辑器设置（能保存的配置） 同步设置时触发
    [AttributeUsage(AttributeTargets.Method)]
    public class OnSyncEditorSettingAttribute : OrderCallBack
    {
        public OnSyncEditorSettingAttribute()
            : base(0)
        {
        }

        public OnSyncEditorSettingAttribute(int order)
            : base(order)
        {
        }
    }

    // 编译完成，第一次调用PostProcessScene时
    public class OnCompileCompleteAttribute : OrderCallBack
    {
        public OnCompileCompleteAttribute()
            : base(0)
        {
        }

        public OnCompileCompleteAttribute(int order)
            : base(order)
        {
        }
    }
}
