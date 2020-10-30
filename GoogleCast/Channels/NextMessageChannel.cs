using System;
using System.Collections.Generic;
using System.Text;

namespace GoogleCast.Channels
{
    class NextMessageChannel : Channel, INextMessageChannel
    {
        public NextMessageChannel() : base("next")
        {

        }
    }
}
