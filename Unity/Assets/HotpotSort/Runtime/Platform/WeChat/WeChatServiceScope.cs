using System;
using HotpotSort.Contracts;

namespace HotpotSort.Platform
{
    // Created only after SDK initialization. Owns native service lifetimes, never UI.
    public sealed class WeChatServiceScope:IDisposable
    {
        public WeChatRuntimeConfig Config { get; }
        public IWeChatRewardRuntime Runtime { get; }
        public WeChatShareService Share { get; }
        public WeChatActivityLinkService ActivityLinks { get; }
        public WeChatRewardService Rewards { get; }
        public IWeChatFriendBoardSurface Friends { get; }
        public bool MenuRegistered { get; }
        bool disposed;
        public WeChatServiceScope(WeChatRuntimeConfig config,IWeChatRewardRuntime runtime=null,Func<IWeChatFriendBoardSurface> friendFactory=null)
        {
            Config=config??throw new ArgumentNullException(nameof(config));Runtime=runtime??new WeChatRewardLifecycle();
            try
            {
                Share=new WeChatShareService(Config,Runtime);Rewards=new WeChatRewardService(Config,Runtime,Share);
                ActivityLinks=new WeChatActivityLinkService(Config);
                MenuRegistered=Share.RegisterMenu();
                if(Runtime.Available&&Config.FriendBoardState==WeChatCapabilityState.Ready)Friends=friendFactory!=null?friendFactory():new WeChatFriendBoardSurface();
            }
            catch{Dispose();throw;}
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;
            ActivityLinks?.Dispose();
            try{Friends?.Dispose();}
            finally{try{Rewards?.Dispose();}finally{try{Share?.Dispose();}finally{(Runtime as IDisposable)?.Dispose();}}}
        }
    }
}
