using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Sockets;

/// <summary>
/// 
/// This class create a cube
/// 
/// Author: Qiaofeng Wang &  Rizwan Mohammud
/// </summary>
namespace AgCubio
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Cube
    {
        [JsonProperty]
        public double loc_x { get; set; }
        [JsonProperty]
        public double loc_y { get; set; }
        [JsonProperty]
        public int argb_color { get; set; }
        [JsonProperty]
        public long uid { get; set; }
        [JsonProperty]
        public bool food { get; set; }
        [JsonProperty]
        public string Name { get; set; }
        [JsonProperty]
        public double Mass { get; set; }
        [JsonProperty]
        public long team_id { get; set; }

        public float momentumX { get; set; }

        public float momentumY { get; set; }

        public float mbl { get; set; }

        public bool merge = true;

        public DateTime mergeTime;

        public bool virus = false;

        [JsonConstructor]
        public Cube(double loc_x, double loc_y, int color, long uid, bool food, string name, double mass)
        {
            this.loc_x = loc_x;
            this.loc_y = loc_y;
            this.argb_color = color;
            this.uid = uid;
            this.food = food;
            this.Name = name;
            this.Mass = mass;
        }

        public float getWidth()
        {
            return (float)Math.Pow(Mass, 0.63);
        }

        public bool inside(Cube other)
        {
            return loc_x < other.loc_x + (other.getWidth() / 2) && loc_x > other.loc_x - (other.getWidth() / 2) && loc_y < other.loc_y + (other.getWidth() / 2) && loc_y > other.loc_y - (other.getWidth() / 2);
        }

        public void apply_momentum()
        {
            if (mbl < 0)
                return;

            mbl -= 1;
            loc_x += momentumX;
            loc_y += momentumY;
        }

        public void attrition(double attritionRate)
        {
            if (Mass > 200)
                Mass -= Math.Sqrt(Mass) / attritionRate;
        }

        public bool Equals(Cube cube)
        {
            return uid == cube.uid;
        }

        public override int GetHashCode()
        {
            return (int)uid;
        }

        public bool overlap(LinkedList<Cube> others, out float min_x_overlap, out float min_y_overlap, out float com_x, out float com_y)
        {
            min_x_overlap = 0;
            min_y_overlap = 0;
            int num = 1;
            com_x = (float)loc_x;
            com_y = (float)loc_y;
            bool result = false;
            foreach (Cube current in others)
            {
                if (current.uid != this.uid && (!current.merge || !this.merge))
                {
                    float num2 = (this.getWidth() + current.getWidth() + 2) / 2 - (float)Math.Abs(loc_x - (float)current.loc_x);
                    float num3 = (this.getWidth() + current.getWidth() + 2) / 2 - (float)Math.Abs(loc_y - (float)current.loc_y);
                    if (num2 > 0f && num3 > 0f)
                    {
                        result = true;
                        com_x += (float)current.loc_x;
                        com_y += (float)current.loc_y;
                        num++;
                        if (num2 > num3)
                        {
                            min_y_overlap = num3;
                        }
                        else
                        {
                            min_x_overlap = num2;
                        }
                    }
                }
            }
            com_x /= (float)num;
            com_y /= (float)num;
            return result;
        }
    }
}