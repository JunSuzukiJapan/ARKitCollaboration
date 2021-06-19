#import <MultipeerConnectivity/MultipeerConnectivity.h>

typedef void (*DidChangePeerStateHandlerCaller)(MCSessionState state, void* methodHandle);

@interface MultipeerDelegate : NSObject<MCSessionDelegate, MCNearbyServiceAdvertiserDelegate, MCNearbyServiceBrowserDelegate>

- (nullable instancetype)initWithName:(nonnull NSString *)name serviceType:(nonnull NSString*)serviceType
                                                 didChangePeerStateHandler:(void*)didChangePeerStateHandler
                                           didChangePeerStateHandlerCaller:(DidChangePeerStateHandlerCaller)didChangePeerStateHandlerCaller;
- (nullable NSError*)sendToAllPeers:(nonnull NSData*)data withMode:(MCSessionSendDataMode)mode;
- (NSUInteger)connectedPeerCount;
- (NSUInteger)queueSize;
- (nonnull NSData*)dequeue;

@property BOOL enabled;

@end
