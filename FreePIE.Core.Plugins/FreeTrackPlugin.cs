﻿using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using FreePIE.Core.Contracts;

namespace FreePIE.Core.Plugins
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FreeTrackData
    {
        public int DataID;
        public int CamWidth;
        public int CamHeight;
        // virtual pose
        public float Yaw;   // positive yaw to the left
        public float Pitch; // positive pitch up
        public float Roll;  // positive roll to the left
        public float X;
        public float Y;
        public float Z;
        // raw pose with no smoothing, sensitivity, response curve etc. 
        public float RawYaw;
        public float RawPitch;
        public float RawRoll;
        public float RawX;
        public float RawY;
        public float RawZ;
        // raw points, sorted by Y, origin top left corner
        public float X1;
        public float Y1;
        public float X2;
        public float Y2;
        public float X3;
        public float Y3;
        public float X4;
        public float Y4;
    };

    [LuaGlobalType(Type = typeof(FreeTrackGlobal))]
    public class FreeTrackPlugin : IOPlugin
    {
        private MemoryMappedFile memoryMappedFile;
        private MemoryMappedViewAccessor accessor;
        public FreeTrackData WriteData { get; set; }

        public object CreateGlobal()
        {
            return new FreeTrackGlobal(this);
        }

        public Action Start()
        {
            memoryMappedFile = MemoryMappedFile.CreateOrOpen("FT_SharedMem", Marshal.SizeOf(typeof(FreeTrackData)));
            accessor = memoryMappedFile.CreateViewAccessor();
            
            OnStarted(this, new EventArgs());

            return null;
        }

        public void Stop()
        {
            accessor.Dispose();
            memoryMappedFile.Dispose();
        }

        protected virtual void OnStarted(object sender, EventArgs e)
        {
            if(Started != null)
            {
                Started(sender, e);
            }
        }

        public bool GetProperty(int index, IPluginProperty property)
        {
            return false;
        }

        public bool SetProperties(Dictionary<string, object> properties)
        {
            return false;
        }

        public event EventHandler Started;

        public FreeTrackData Read()
        {
            FreeTrackData local;
            accessor.Read(0, out local);
            return local;
        }

        public void Write()
        {
            var local = WriteData;
            local.DataID++;
            accessor.Write(0, ref local);
        }
    }

    [LuaGlobal(Name = "freeTrack")]
    public class FreeTrackGlobal
    {
        private readonly FreeTrackPlugin plugin;

        public FreeTrackGlobal(FreeTrackPlugin plugin)
        {
            this.plugin = plugin;
        }

        public float getYaw()
        {
            return plugin.Read().Yaw;
        }

        public float getPitch()
        {
            return plugin.Read().Pitch;
        }

        public float getRoll()
        {
            return plugin.Read().Roll;
        }

        public float getX()
        {
            return plugin.Read().X;
        }

        public float getY()
        {
            return plugin.Read().Y;
        }

        public float getZ()
        {
            return plugin.Read().Z;
        }
    }
}