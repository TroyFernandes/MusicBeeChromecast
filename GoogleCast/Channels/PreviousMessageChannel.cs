using GoogleCast.Messages;
using System;
using System.Threading.Tasks;

namespace GoogleCast.Channels
{
    class PreviousMessageChannel : Channel, IPreviousMessageChannel
    {
        public PreviousMessageChannel() : base("previous")
        {
        }


    }

}
