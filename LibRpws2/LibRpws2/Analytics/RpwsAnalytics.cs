using System;
using System.Collections.Generic;
using System.Text;

namespace LibRpws2.Analytics
{
    /// <summary>
    /// Used to record analytics over the execution time of a function.
    /// </summary>
    public class RpwsAnalytics
    {
        /// <summary>
        /// Time sesson started
        /// </summary>
        public DateTime start;

        /// <summary>
        /// Time session ended.
        /// </summary>
        public DateTime end;

        /// <summary>
        /// Master name
        /// </summary>
        public string name;

        /// <summary>
        /// Latest checkpoint, hidden from the user.
        /// </summary>
        private RpwsAnalyticsCheckpoint latest_checkpoint;

        /// <summary>
        /// Checkpoints.
        /// </summary>
        public List<RpwsAnalyticsCheckpoint> checkpoints = new List<RpwsAnalyticsCheckpoint>();

        public RpwsAnalytics(string masterName, string checkpointName)
        {
            //Create object and then start.
            start = DateTime.UtcNow;
            name = masterName;
            //Create checkpoint
            CreateNewCheckpoint(checkpointName);
        }

        private readonly ConsoleColor[] pretty_colors = new ConsoleColor[]
        {
            ConsoleColor.Blue,
            ConsoleColor.Cyan,
            ConsoleColor.Gray,
            ConsoleColor.Green,
            ConsoleColor.Magenta,
            ConsoleColor.Red,
            ConsoleColor.White,
            ConsoleColor.Yellow
        };

        /// <summary>
        /// Dump everything to the console.
        /// </summary>
        public void DumpToConsole()
        {
            Console.WriteLine("RPWS ANALYTICS DUMP - " + name);
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("");
            int[] total_percentages = CalculatePercentages();
            //Produce bars
            for(int i = 0; i<total_percentages.Length; i++)
            {
                Console.BackgroundColor = pretty_colors[i % pretty_colors.Length];
                int adjusted_percent = total_percentages[i] / 3;
                for (int j = 0; j < adjusted_percent; j++)
                    Console.Write(" ");
            }
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write("\n\n");
            //Produce key
            for (int i = 0; i < total_percentages.Length; i++)
            {
                Console.BackgroundColor = pretty_colors[i % pretty_colors.Length];
                RpwsAnalyticsCheckpoint c = checkpoints[i];
                int adjusted_percent = total_percentages[i] / 3;
                Console.Write($"{c.name} - {new TimeSpan(c.total_time).TotalMilliseconds} ms - {total_percentages[i]}% - {c.reps.Count} reps - {new TimeSpan(c.total_time / (long)c.reps.Count).TotalMilliseconds} ms per rep average\n");
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
        }

        /// <summary>
        /// Calculate the percentages for each of the checkpoints.
        /// </summary>
        public int[] CalculatePercentages()
        {
            int[] output = new int[checkpoints.Count];
            long total_length = (end - start).Ticks;

            for(int i = 0; i<checkpoints.Count; i++)
            {
                RpwsAnalyticsCheckpoint checkpoint = checkpoints[i];
                long checkpoint_length = checkpoint.total_time;
                float percent = (float)checkpoint_length / (float)total_length;
                output[i] = (int)(percent * 100);
            }

            return output;
        }

        private void CreateNewCheckpoint(string name)
        {
            latest_checkpoint = new RpwsAnalyticsCheckpoint
            {
                name = name,
                reps = new List<RpwsAnalyticsCheckpointRep>()
            };
            //Add new rep
            latest_checkpoint.reps.Add(new RpwsAnalyticsCheckpointRep
            {
                start = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Save the last checkpoint and do not create a new one.
        /// </summary>
        public void End()
        {
            //End current rep
            latest_checkpoint.EndCurrentRep();
            //Move final checkpoint to main list.
            checkpoints.Add(latest_checkpoint);
            latest_checkpoint = null;
            //Move end time
            end = DateTime.UtcNow;
        }

        /// <summary>
        /// Ends the current checkpoint and continues.
        /// </summary>
        /// <param name="name"></param>
        public void NextCheckpoint(string name)
        {
            //End current
            End();
            //Create new 
            CreateNewCheckpoint(name);
        }

        /// <summary>
        /// Adds a new rep to the current checkpoint.
        /// </summary>
        public void NextCheckpointRep()
        {
            //End current rep
            latest_checkpoint.EndCurrentRep();
            //Add new rep
            latest_checkpoint.reps.Add(new RpwsAnalyticsCheckpointRep
            {
                start = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Checkpoint in a bigger session.
    /// </summary>
    public class RpwsAnalyticsCheckpoint
    {
        public List<RpwsAnalyticsCheckpointRep> reps = new List<RpwsAnalyticsCheckpointRep>();
        public string name;

        public RpwsAnalyticsCheckpointRep latest_rep {
            get
            {
                return reps[reps.Count - 1];
            }
            set
            {
                reps[reps.Count - 1] = value;
            }
        }

        public RpwsAnalyticsCheckpointRep first_rep
        {
            get
            {
                return reps[0];
            }
            set
            {
                reps[0] = value;
            }
        }

        public long total_time
        {
            get
            {
                return latest_rep.end.Ticks - first_rep.start.Ticks;
            }
        }

        public void EndCurrentRep()
        {
            latest_rep.end = DateTime.UtcNow;
        }

        public TimeSpan GetAverage()
        {
            long averageTicks = 0;
            //Add up all ticks
            foreach (var r in reps)
                averageTicks += (r.end - r.start).Ticks;
            //Divide
            averageTicks /= reps.Count;
            //Convert back and return
            return new TimeSpan(averageTicks);
        }
    }

    /// <summary>
    /// Rep in checkpoint.
    /// </summary>
    public class RpwsAnalyticsCheckpointRep
    {
        public DateTime start;
        public DateTime end;
    }
}
