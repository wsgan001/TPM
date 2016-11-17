﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NDesk.Options;
using LK.OSMUtils.OSMDatabase;
using LK.TMatch;
using LK.GPXUtils;
using System.IO;

namespace LK.TRoute
{
    class Program
    {
        static void Main(string[] args)
        {
            string osmPath = "";
            int eps = -1;
            int minTraffic = -1;
            bool showHelp = false;

            OptionSet parameters = new OptionSet() {
                { "osm=", "path to the map file",                                                    v => osmPath = v},
                { "eps=",   "size of the eps-neighborhood to be considered (integer)",               v => eps = Convert.ToInt32(v)},
                { "minTraffic=", "minimum traffic considered (integer)",                             v => minTraffic = Convert.ToInt32(v)},
                { "h|?|help",                                                                        v => showHelp = v != null},
            };

            try
            {
                parameters.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("TRoute: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `TRoute --help' for more information.");
                return;
            }
            
            if (showHelp || string.IsNullOrEmpty(osmPath) || eps < 0 || minTraffic < 0)
            {
                ShowHelp(parameters);
                return;
            }
            
            var osmFile = new OSMDB();
            osmFile.Load(osmPath);
            var roadGraph = new RoadGraph();
            roadGraph.Build(osmFile);
            
            var hotRoutes = new FlowScan().Run(roadGraph, eps, minTraffic);


            //OSMDB hotRouteOutput = new OSMDB();
            //List<OSMNode> hotRouteNodesList = new List<OSMNode>();

            /*foreach (var hr in hotRoutes)
            {
                foreach (var seg in hr.Segments)
                {
                    OSMNode nd = new OSMNode();
                    string line = seg.From.MapPoint.Latitude + "," + seg.From.MapPoint.Longitude + " " + seg.To.MapPoint.Latitude
                        + "," + seg.To.MapPoint.Longitude;
                    csv.AppendLine(line);
                }
            }*/


            // Saving GPX file of the Hot Route
            List<GPXPoint> listPoints;
            List<GPXTrackSegment> listSegments;
            GPXTrackSegment segTrack;

            List<GPXTrack> track = new List<GPXTrack>();
            GPXTrack tr;

            Console.WriteLine(hotRoutes.Count);
            foreach (var hr in hotRoutes)
            {
                listSegments = new List<GPXTrackSegment>();

                foreach (var seg in hr.Segments)
                {
                    listPoints = new List<GPXPoint>();
                    
                    foreach (var segInception in seg.Geometry.Segments)
                    {
                        GPXPoint start = new GPXPoint() { Latitude = segInception.StartPoint.Latitude, Longitude = segInception.StartPoint.Longitude };
                        GPXPoint end = new GPXPoint() { Latitude = segInception.EndPoint.Latitude, Longitude = segInception.EndPoint.Longitude };
                        listPoints.Add(start);
                        listPoints.Add(end);
                    }

                    segTrack = new GPXTrackSegment(listPoints);
                    listSegments.Add(segTrack);
                }
                
                tr = new GPXTrack();
                tr.Segments.AddRange(listSegments);
                track.Add(tr);

            }
            var gpx = new GPXDocument() { Tracks = track };
            gpx.Save("HotRoute.gpx");

        }

        /// <summary>
        /// Prints a help message
        /// </summary>
        /// <param name="p">The parameters accepted by this program</param>
        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: TRoute [OPTIONS]+");
            Console.WriteLine("Outputs hot routes found");
            Console.WriteLine();

            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }
    }
}