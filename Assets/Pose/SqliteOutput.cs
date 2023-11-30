#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using UnityEngine;

namespace Pose
{
    public class SqliteOutput : MonoBehaviour
    {
        public string DbPath;

        public void Serialize(int role, List<List<Vector2>?> allPoses, int frameCount)
        {
            string cs = $"URI=file:{DbPath}";
            using SQLiteConnection conn = new SQLiteConnection(cs);
            conn.Open();

            using IDbCommand cmd = conn.CreateCommand();
            using IDbTransaction transaction = conn.BeginTransaction();

            int frameId = 0;
            int cameraId = 0;

            int numPoses = Enum.GetNames(typeof(Joints)).Length;
            foreach (List<Vector2>? pose in allPoses)
            {
                if (pose == null)
                {
                    frameId++;

                    if (frameId == frameCount)
                    {
                        frameId = 0;
                        cameraId++;
                    }

                    continue;
                }

                int i = 0;
                foreach (Vector2 position in pose)
                {
                    cmd.CommandText =
                        "UPDATE" + (role == 0 ? " lead " : " follow ") +
                        $"SET position_x = {(int)position.x}, position_y = {-(int)position.y} " +
                        $"WHERE id = {cameraId * frameCount * numPoses + frameId * numPoses + i + 1}";

                    cmd.ExecuteNonQuery();

                    i++;
                    if (i == numPoses)
                    {
                        frameId++;

                        if (frameId == frameCount)
                        {
                            frameId = 0;
                            cameraId++;
                        }
                    }
                }
            }

            transaction.Commit();
        }
    }
}