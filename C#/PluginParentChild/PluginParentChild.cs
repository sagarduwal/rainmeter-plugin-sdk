﻿/*
  Copyright (C) 2012 Birunthan Mohanathas

  This program is free software; you can redistribute it and/or
  modify it under the terms of the GNU General Public License
  as published by the Free Software Foundation; either version 2
  of the License, or (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with this program; if not, write to the Free Software
  Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
*/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Rainmeter;

// Overview: This example demonstrates a basic implementation of a parent/child
// measure structure. In this particular example, we have a "parent" measure
// which contains the values for the options "ValueA", "ValueB", and "ValueC".
// The child measures are used to return a specific value from the parent.

// Use case: You could, for example, have a "main" parent measure that queries
// information some data set. The child measures can then be used to return
// specific information from the data queried by the parent measure.

// Sample skin:
/*
    [Rainmeter]
    Update=1000
    BackgroundMode=2
    SolidColor=000000

    [mParent]
    Measure=Plugin
    Plugin=ParentChild.dll
    ValueA=111
    ValueB=222
    ValueC=333
    Type=A

    [mChild1]
    Measure=Plugin
    Plugin=ParentChild.dll
    ParentName=mParent
    Type=B

    [mChild2]
    Measure=Plugin
    Plugin=ParentChild.dll
    ParentName=mParent
    Type=C

    [Text]
    Meter=STRING
    MeasureName=mParent
    MeasureName2=mChild1
    MeasureName3=mChild2
    X=5
    Y=5
    W=200
    H=55
    FontColor=FFFFFF
    Text="mParent: %1#CRLF#mChild1: %2#CRLF#mChild2: %3"
*/

namespace PluginParentChild
{
    internal class Measure
    {
        internal enum MeasureType
        {
            A,
            B,
            C
        }

        internal MeasureType Type = MeasureType.A;

        internal virtual void Reload(Rainmeter.API api, ref double maxValue)
        {
            string type = api.ReadString("Type", "");
            switch (type.ToLowerInvariant())
            {
                case "a":
                    Type = MeasureType.A;
                    break;

                case "b":
                    Type = MeasureType.B;
                    break;

                case "c":
                    Type = MeasureType.C;
                    break;

                default:
                    API.Log(API.LogType.Error, "ParentChild.dll: Type=" + type + " not valid");
                    break;
            }
        }

        internal virtual double Update()
        {
            return 0.0;
        }
    }

    internal class ParentMeasure : Measure
    {
        internal string Name;
        internal IntPtr Skin;

        internal int ValueA;
        internal int ValueB;
        internal int ValueC;

        internal override void Reload(Rainmeter.API api, ref double maxValue)
        {
            base.Reload(api, ref maxValue);

            Name = api.GetMeasureName();
            Skin = api.GetSkin();

            ValueA = api.ReadInt("ValueA", 0);
            ValueB = api.ReadInt("ValueB", 0);
            ValueC = api.ReadInt("ValueC", 0);
        }

        internal override double Update()
        {
            return GetValue(Type);
        }

        internal double GetValue(MeasureType type)
        {
            switch (type)
            {
                case MeasureType.A:
                    return ValueA;

                case MeasureType.B:
                    return ValueB;

                case MeasureType.C:
                    return ValueC;
            }

            return 0.0;
        }
    }

    internal class ChildMeasure : Measure
    {
        private bool HasParent = false;
        private uint ParentID;

        internal override void Reload(Rainmeter.API api, ref double maxValue)
        {
            base.Reload(api, ref maxValue);

            string parentName = api.ReadString("ParentName", "");
            IntPtr skin = api.GetSkin();

            // Find parent using name AND the skin handle to be sure that it's the right one
            RuntimeTypeHandle parentType = typeof(ParentMeasure).TypeHandle;
            foreach (KeyValuePair<uint, Measure> pair in Plugin.Measures)
            {
                if (System.Type.GetTypeHandle(pair.Value).Equals(parentType))
                {
                    ParentMeasure parentMeasure = (ParentMeasure)pair.Value;
                    if (parentMeasure.Name.Equals(parentName) &&
                        parentMeasure.Skin.Equals(skin))
                    {
                        HasParent = true;
                        ParentID = pair.Key;
                        return;
                    }
                }
            }

            HasParent = false;
            API.Log(API.LogType.Error, "ParentChild.dll: ParentName=" + parentName + " not valid");
        }

        internal override double Update()
        {
            if (HasParent)
            {
                ParentMeasure parent = (ParentMeasure)Plugin.Measures[ParentID];
                return parent.GetValue(Type);
            }

            return 0.0;
        }
    }

    public static class Plugin
    {
        internal static Dictionary<uint, Measure> Measures = new Dictionary<uint, Measure>();

        [DllExport]
        public unsafe static void Initialize(void** data, void* rm)
        {
            uint id = (uint)((void*)*data);
            Rainmeter.API api = new Rainmeter.API((IntPtr)rm);

            string parent = api.ReadString("ParentName", "");
            if (String.IsNullOrEmpty(parent))
            {
                Measures.Add(id, new ParentMeasure());
            }
            else
            {
                Measures.Add(id, new ChildMeasure());
            }
        }

        [DllExport]
        public unsafe static void Finalize(void* data)
        {
            uint id = (uint)data;
            Measures.Remove(id);
        }

        [DllExport]
        public unsafe static void Reload(void* data, void* rm, double* maxValue)
        {
            uint id = (uint)data;
            Measures[id].Reload(new Rainmeter.API((IntPtr)rm), ref *maxValue);
        }

        [DllExport]
        public unsafe static double Update(void* data)
        {
            uint id = (uint)data;
            return Measures[id].Update();
        }
    }
}
