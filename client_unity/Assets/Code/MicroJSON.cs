/**!
 * Papika telemetry client (Unity) library.
 * Copyright 2015 Kristin Siu (kasiu).
 * Revision Id: UNKNOWN_REVISION_ID
 */
using System.Collections.Generic;

namespace Papika
{
    /// <summary>
    /// A mini (micro) JSON serializer used to encode JSON objects for the Papika client.
    /// Not as robust as other options (but is free and tiny).
    /// </summary>
    public static class MicroJSON
    {
        /// <summary>
        /// Serialize an object to a text writer.
        /// Handles basic types - if a type is unsupported, it isn't written.
        /// </summary>
        public static void SerializeTo(object json, System.IO.TextWriter writer) {
            // NULL
            if (json == null) {
                writer.Write("null");
                return;
            }
            // int
            var jInt = json as int?;
            if (jInt != null) {
                writer.Write(jInt.Value);
                return;
            }
            // short
            var jShort = json as short?;
            if (jShort != null) {
                writer.Write(jShort.Value);
                return;
            }
            // string
            var jStr = json as string;
            if (jStr != null) {
                writer.Write('\"');
                writer.Write(jStr.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\t", "\\t").Replace("\r", "\\r").Replace("\"", "\\\""));
                writer.Write('\"');
                return;
            }
            // guids (MAGIC SPECIAL CASING BECAUSE WE USE THEM SO FREQUENTLY)
            var jGuid = json as System.Guid?;
            if (jGuid != null) {
                SerializeTo(jGuid.Value.ToString(), writer);
            }
            // boolean
            var jBool = json as bool?;
            if (jBool != null) {
                writer.Write((jBool.Value) ? "true" : "false");
                return;
            }
            // array
            var jArray = json as object[];
            if (jArray != null) {
                writer.Write('[');
                for (var i = 0; i < jArray.Length; i++) {
                    if (i > 0) {
                        writer.Write(", ");
                    }
                    SerializeTo(jArray[i], writer);
                }
                writer.Write(']');
                return;
            }
            // object
            var jDict = json as IDictionary<string, object>;
            if (jDict != null) {
                writer.Write('{');
                var i = 0;
                foreach (var kvp in jDict) {
                    if (i > 0) {
                        writer.Write(", ");
                    }
                    writer.Write('"');
                    writer.Write(kvp.Key);
                    writer.Write('"');
                    writer.Write(':');
                    SerializeTo(kvp.Value, writer);
                    i++;
                }
                writer.Write('}');
                return;
            }
            // TODO (kasiu): Handle other kinds of types in the future.
        }

        /// <summary>
        /// Serializes an object as a string.
        /// </summary>
        public static string Serialize(object json) {
            var sb = new System.Text.StringBuilder();
            var sw = new System.IO.StringWriter(sb);
            SerializeTo(json, sw);
            return sw.ToString();
        }
    }
}