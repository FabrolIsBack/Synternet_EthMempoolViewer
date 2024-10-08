using NatsProvidersub;

//Coding for test the SDK, in this case all commented because the class NatsProvider will be use from MainWindows EthMempool

String accessToken = "{accessToken}";
String natsUrl = "nats://broker-eu-03.synternet.com"; //"{nats://url}";
String streamName = "synternet.ethereum.mempool";// {streamName}";
NatsProvider natsProvider = new NatsProvider(accessToken, natsUrl, streamName);
natsProvider.Connect();

/**
* if you want to subscribe messages*
**/
//Test if arrives the flow
for(int i = 0; i < 3; i++)
{
    natsProvider.SubscribeSync();
}

natsProvider.close();
Console.ReadKey();


/**
* if you want to publish something:

string msg = "Hello World";
natsProvider.Publish(msg);
natsProvider.close();
**/