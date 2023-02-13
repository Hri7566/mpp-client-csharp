using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mpp_client_csharp
{
    public class User
    {
        public string _id;
        public string name;
        public string color;

        public User(string _id, string name, string color)
        {
            this._id = _id;
            this.name = name;
            this.color = color;
        }
    }

    public class Participant : User
    {
        public string id;
        public float? x;
        public float? y;

        public Participant(string _id, string name, string color, string id, float? x, float? y) : base(_id, name, color)
        {
            this.id = id;
            this.x = x;
            this.y = y;
        }
    }
}
