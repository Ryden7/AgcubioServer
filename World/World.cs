using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

/// <summary>
/// 
/// This class create a world for all cubes
/// 
/// Author: Qiaofeng Wang &  Rizwan Mohammud
/// </summary>
namespace AgCubio
{
    public class World
    {
        public Dictionary<long, Cube> players;

        public Dictionary<long, Cube> foods;

        public Dictionary<long, LinkedList<Cube>> splits;

        //private bool splitOrNot = false;

        //Width, Height - the game size of the world
        //Heartbeats per second - how many updates the server should attempt to execute per second.Note: adequate work will simply update the world as fast as possible.
        //Top speed - how fast cubes can move when small
        //Low speed - how fast the biggest cubes move
        //Attrition rate - how fast cubes lose mass
        //Food value - the default mass of food
        //Player start mass - the default starting mass of the player.
        //Max food - how many food to maintain in the world.Server should update one food per heartbeat if below this.
        //Minimum split mass - players are not allowed to split if below this mass
        //Maximum split distance - how far a cube can be "thrown" when split
        //Maximum splits - how many total cubes a single player is allowed.Note: our test server does not implement this. Try setting it to around 10-20.
        //Absorb distance delta - how close cubes have to be in order for the larger to eat the smaller


        private readonly int width = 1000;
        private readonly int height = 1000;

        // test for how fast
        public readonly int hps = 30;

        private readonly double TopSpeed = 5;
        private readonly double LowSpeed = 0.5;

        private readonly int attritionRate = 200;

        private readonly int FoodValue = 1;
        private readonly int startMass = 1000;
        private readonly int maxFood = 5000;

        private readonly int msm = 100;
        private readonly int msd = 150;
        private readonly int maxSplits = 15;

        // half of the cube
        private readonly double absorbDistance = 1.25;
        public int heartbeats = 0;

        private Random r;

        public World()
        {
            players = new Dictionary<long, Cube>();
            foods = new Dictionary<long, Cube>();
            splits = new Dictionary<long, LinkedList<Cube>>();
            r = new Random();

            Cube virus1 = new Cube((double)r.Next(width), (double)r.Next(height), Color.Green.ToArgb(), r.Next(), false, "", 250);
            foods[virus1.uid] = virus1;

            Cube virus2 = new Cube((double)r.Next(width), (double)r.Next(height), Color.Green.ToArgb(), r.Next(), false, "", 250);
            foods[virus2.uid] = virus2;
        }

        public Cube add_player(string name)
        {
            Cube result;
            lock (this)
            {
                // a random color for new player
                Color color = Color.FromArgb(r.Next(255), r.Next(255), r.Next(255));
                result = new Cube((double)r.Next(width), (double)r.Next(height), color.ToArgb(), r.Next(), false, name, startMass);
                this.players[result.uid] = result;
            }
            return result;
        }

        public void attrition()
        {
            lock (this)
            {
                foreach (Cube temp in this.players.Values)
                    temp.attrition(attritionRate);
            }
        }

        public void split(long uid, int x, int y)
        {
            LinkedList<Cube> list = new LinkedList<Cube>();
            if (!splits.TryGetValue(uid, out list))
            {
                //list = splits[uid];
                Cube cube = players[uid];

                list = new LinkedList<Cube>();
                //list.Clear();
                list.AddFirst(cube);

                cube.team_id = cube.uid;
            }

            LinkedList<Cube> tempList = new LinkedList<Cube>();

            foreach (Cube cube in list)
            {
                tempList.AddFirst(cube);
                if (cube.Mass < msm)
                    return;

                float num = x - (float)cube.loc_x;
                float num2 = y - (float)cube.loc_y;
                float num3 = (float)Math.Sqrt((num * num + num2 * num2));
                float num4 = cube.getWidth() * 5f / 120f;
                num = num / num3 * num4;
                num2 = num2 / num3 * num4;
                cube.Mass /= 2.0;
                cube.merge = false;
                cube.mergeTime = DateTime.Now.AddSeconds(10.0 + cube.Mass / 100.0);

                Cube tempCube = new Cube(cube.loc_x, cube.loc_y, cube.argb_color, r.Next(), false, cube.Name, cube.Mass);
                tempCube.momentumX = num;
                tempCube.momentumY = num2;
                tempCube.mbl = hps;

                tempCube.merge = false;
                tempCube.mergeTime = DateTime.Now.AddSeconds(10.0 + cube.Mass / 100.0);
                tempCube.team_id = cube.team_id;
                players[tempCube.uid] = tempCube;

                tempList.AddFirst(tempCube);
            }

            if (tempList.Count > 1)
                splits[uid] = tempList;
        }

        public LinkedList<Cube> eat()
        {
            LinkedList<Cube> linkedList = new LinkedList<Cube>();
            lock (this)
            {
                foreach (Cube current in this.players.Values)
                {
                    foreach (Cube current2 in this.players.Values)
                    {
                        if (current != current2)
                        {
                            if (current.team_id == current2.team_id)
                            {
                                if (current.merge && current2.merge && current2.inside(current))
                                {
                                    if (current2.uid == current2.team_id)
                                    {
                                        linkedList.AddFirst(current);
                                        current2.Mass += current.Mass;
                                        current.Mass = 0.0;
                                    }
                                    else
                                    {
                                        linkedList.AddFirst(current2);
                                        current.Mass += current2.Mass;
                                        current2.Mass = 0.0;
                                    }
                                }
                            }
                            else if (current.Mass > current2.Mass * absorbDistance && current2.inside(current))
                            {
                                linkedList.AddFirst(current2);
                                current.Mass += current2.Mass;
                                current2.Mass = 0.0;
                            }
                        }
                    }
                }

                // erase the dead cubes.
                foreach (Cube temp in linkedList)
                {
                    this.players.Remove(temp.uid);
                    LinkedList<Cube> dead = new LinkedList<Cube>();

                    if (splits.TryGetValue(temp.team_id, out dead))
                    {
                        //dead = splits[temp.team_id];

                        if (dead.Count < 2)
                            return null;

                        dead.Remove(temp);

                        if (temp.uid == temp.team_id)
                        {
                            Cube cube = dead.First<Cube>();
                            players.Remove(cube.uid);
                            players.Add(temp.team_id, cube);
                            temp.uid = cube.uid;
                            cube.uid = temp.team_id;
                        }

                        if (dead.Count == 1)
                            splits.Remove(temp.team_id);

                        dead = null;
                    }
                }
            }
            return linkedList;
        }

        public void movePlayer(long uid, int target_x, int target_y)
        {
            LinkedList<Cube> linkedList;
            if (splits.TryGetValue(uid, out linkedList))
            {
                foreach (Cube current in linkedList)
                {
                    current.apply_momentum();
                    move(current, target_x, target_y);
                }

                DateTime now = DateTime.Now;
                foreach (Cube current in linkedList)
                {
                    if (DateTime.Compare(now, current.mergeTime) > 0)
                    {
                        current.merge = true;
                    }
                    float num;
                    float num2;
                    float num3;
                    float num4;
                    if (current.overlap(linkedList, out num, out num2, out num3, out num4))
                    {
                        num = Math.Min(num, 10f);
                        num2 = Math.Min(num2, 10f);
                        if (num3 > current.loc_x)
                        {
                            current.loc_x -= num;
                        }
                        else
                        {
                            current.loc_x += num;
                        }
                        if (num4 > current.loc_y)
                        {
                            current.loc_y -= num2;
                        }
                        else
                        {
                            current.loc_y += num2;
                        }
                    }
                }

                return;
            }
            Cube cube;
            if (players.TryGetValue(uid, out cube))
                move(players[uid], target_x, target_y);

        }

        public void move(Cube player, int x, int y)
        {
            float num = x - (float)player.loc_x;
            float num2 = y - (float)player.loc_y;
            float num3 = (float)Math.Sqrt((num * num + num2 * num2));
            float num4 = (float)TopSpeed - (float)(player.Mass / 500.0);

            if (Math.Abs(num) < 10f && Math.Abs(num2) < 10f)
                return;

            if (num4 < LowSpeed)
            {
                num4 = (float)LowSpeed;
            }

            //float arg_6D_0 = (float)player.loc_x;
            //float arg_74_0 = (float)player.loc_y;
            num = num / num3 * num4;
            num2 = num2 / num3 * num4;
            player.loc_x += num;
            player.loc_y += num2;

            if (player.loc_x + (player.getWidth() / 2) > width)
                player.loc_x = (-(player.getWidth() / 2) + width) + (float)(r.NextDouble() * 4.0 - 2.0);

            if (player.loc_x - (player.getWidth() / 2) < 0f)
                player.loc_x = (player.getWidth() / 2) + (float)(r.NextDouble() * 2.0 - 1.0);

            if (player.loc_y + (player.getWidth() / 2) > height)
                player.loc_y = (-(player.getWidth() / 2) + height) + (float)(r.NextDouble() * 4.0 - 2.0);

            if (player.loc_y - (player.getWidth() / 2) < 0f)
                player.loc_y = (player.getWidth() / 2) + (float)(r.NextDouble() * 2.0 - 1.0);

        }

        private bool grow_food_in_order;
        public bool food(double chance, out Cube food)
        {
            chance = 1.0;
            bool result;

            int x = 0;
            int y = 0;

            lock (this)
            {
                if (foods.Count < maxFood && r.NextDouble() < chance)
                {
                    if (this.grow_food_in_order)
                    {
                        //food = new Cube((float)this.last_x, (float)this.last_y, (double)FoodValue, Color.FromArgb(this.random.Next(256), this.random.Next(256), this.random.Next(256)).ToArgb(), true, "");
                        food = new Cube(x, y, Color.FromArgb(this.r.Next(256), this.r.Next(256), this.r.Next(256)).ToArgb(), r.Next(), true, "", FoodValue);
                        foods[food.uid] = food;
                        x += 5;
                        if (x > width)
                        {
                            x = 0;
                            y += 5;
                            if (y > height)
                                y = 0;

                        }
                        result = true;
                    }
                    else
                    {
                        //food = new Cube();
                        food = new Cube((r.Next() % width), (r.Next() % height), Color.FromArgb(this.r.Next(256), this.r.Next(256), this.r.Next(256)).ToArgb(), r.Next(), true, "", FoodValue);
                        foods[food.uid] = food;
                        result = true;
                    }
                }
                else
                {
                    food = null;
                    result = false;
                }
            }
            return result;
        }


        public LinkedList<Cube> eatFood()
        {

            LinkedList<Cube> result = new LinkedList<Cube>();
            bool flag = false;

            lock (this)
            {
                long uid = 0;

                LinkedList<Cube> linkedList = new LinkedList<Cube>();
                foreach (Cube current in players.Values)
                {
                    Dictionary<long, Cube>.ValueCollection values = foods.Values;
                    foreach (Cube current2 in values)
                    {
                        if (current2.food == true && current2.virus == false)
                        {
                            if (current.loc_x - (float)(current.getWidth() / 2) < current2.loc_x && current.loc_x + (float)(current.getWidth() / 2) > current2.loc_x && current.loc_y - (float)(current.getWidth() / 2) < current2.loc_y && current.loc_y + (float)(current.getWidth() / 2) > current2.loc_y)
                            {
                                linkedList.AddFirst(current2);
                                current.Mass += current2.Mass;
                                current2.Mass = 0.0;
                            }
                        }
                        else
                        {
                            if (current.loc_x - (float)(current.getWidth() / 2) < current2.loc_x && current.loc_x + (float)(current.getWidth() / 2) > current2.loc_x && current.loc_y - (float)(current.getWidth() / 2) < current2.loc_y && current.loc_y + (float)(current.getWidth() / 2) > current2.loc_y)
                            {
                                flag = true;
                                uid = current.uid;
                                current2.Mass = 0.0;
                                linkedList.AddFirst(current2);
                                //foods.Remove(current2.uid);
                            }
                        }
                    }
                    foreach (Cube current3 in linkedList)
                    {
                        foods.Remove(current3.uid);
                    }
                }

                if (flag == true)
                    split(uid, r.Next(width), r.Next(height));

                result = linkedList;
            }
            return result;
        }
    }
}