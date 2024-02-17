using NATS.Client.Core;

namespace Orleans.Contrib.Streaming.Nats;

public class NatsConfigator
{
    Func<NatsOpts, NatsOpts> configure = opts => opts;
    
    public void AddConfigurator(Func<NatsOpts, NatsOpts> configure)
    {
        this.configure += configure;
    }
    
    public NatsOpts Configure(NatsOpts opts)
    {
        return configure(opts);
    }
}