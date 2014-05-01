﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Depressurizer {

    public enum ValueType {
        Array,
        Value
    }

    /// <summary>
    /// Represents a single node in a Steam config file
    /// </summary>
    public class FileNode {
        public ValueType NodeType;
        // Can be a string or a FileNode, depending on value type.
        public Object NodeData;

        /// <summary>
        /// Gets or sets the subnode with the given key. Can only be used with an array node. If the node does not exist, creates it as an array type.
        /// </summary>
        /// <param name="key">Key to look for or set</param>
        /// <returns></returns>
        /// <exception cref="ApplicationException">Thrown if used on a value node.</exception>
        public FileNode this[string key] {
            get {
                if( this.NodeType == ValueType.Value ) {
                    throw new ApplicationException( string.Format( "Node is a value, not an array. Cannot get key {0}", key ) );
                }
                Dictionary<string, FileNode> arrayData = (Dictionary<string, FileNode>)NodeData;
                if( !arrayData.ContainsKey( key ) ) {
                    arrayData.Add( key, new FileNode() );
                }
                return arrayData[key];
            }
            set {
                if( this.NodeType == ValueType.Value ) {
                    throw new Exception( string.Format( "Node is a value, not an array. Cannot set key {0}", key ) );
                }
                Dictionary<string, FileNode> arrayData = (Dictionary<string, FileNode>)NodeData;
                if( !arrayData.ContainsKey( key ) ) {
                    arrayData.Add( key, value );
                } else {
                    arrayData[key] = value;
                }
            }
        }

        /// <summary>
        /// Quick shortcut for casting data to a a dictionary
        /// </summary>
        public Dictionary<string, FileNode> NodeArray {
            get {
                return ( NodeType == ValueType.Array ) ? ( NodeData as Dictionary<string, FileNode> ) : null;
            }
        }

        public string NodeString {
            get {
                return ( NodeType == ValueType.Value ) ? ( NodeData as string ) : null;
            }
        }

        /// <summary>
        /// Creates a new array-type node
        /// </summary>
        public FileNode() {
            NodeType = ValueType.Array;
            NodeData = new Dictionary<string, FileNode>();
        }

        /// <summary>
        /// Creates a new value-type node
        /// </summary>
        /// <param name="value">Value of the string</param>
        public FileNode( string value ) {
            NodeType = ValueType.Value;
            NodeData = value;
        }

        #region Utility

        /// <summary>
        /// Reads a from the specified stream until it reaches a string terminator (double quote with no escaping slash).
        /// The opening double quote should already be read, and the last one will be discarded.
        /// </summary>
        /// <param name="stream">The stream to read from. After the operation, the stream position will be just past the closing quote.</param>
        /// <returns>The string encapsulated by the quotes.</returns>
        private static string GetStringToken( StreamReader stream ) {
            bool escaped = false;
            bool stringDone = false;
            StringBuilder sb = new StringBuilder();
            char nextChar;
            do {
                nextChar = (char)stream.Read();
                if( escaped ) {
                    switch( nextChar ) {
                        case '\\':
                            sb.Append( '\\' );
                            break;
                        case '"':
                            sb.Append( '"' );
                            break;
                        case '\'':
                            sb.Append( '\'' );
                            break;
                    }
                    escaped = false;
                } else {
                    switch( nextChar ) {
                        case '\\':
                            escaped = true;
                            break;
                        case '"':
                            stringDone = true;
                            break;
                        default:
                            sb.Append( nextChar );
                            break;
                    }
                }
            } while( !stringDone && !stream.EndOfStream );
            if( !stringDone ) {
                if( stream.EndOfStream ) {
                    throw new ParseException( "Unexpected end-of-file reached: Unterminated string." );
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Advances a stream until the next character is not whitespace
        /// </summary>
        /// <param name="stream">The stream to advance</param>
        private static void SkipWhitespace( StreamReader stream ) {
            char nextChar = (char)stream.Peek();
            while( nextChar == ' ' || nextChar == '\r' || nextChar == '\n' || nextChar == '\t' ) {
                stream.Read();
                nextChar = (char)stream.Peek();
            }
        }

        /// <summary>
        /// Writes a string to a stream, adding start/end quotes and escaping any quotes within the string.
        /// </summary>
        /// <param name="stream">Stream to write to</param>
        /// <param name="s">String to write</param>
        private void WriteFormattedString( StreamWriter stream, string s ) {
            stream.Write( "\"" );
            stream.Write( s.Replace( "\"", "\\\"" ) );
            stream.Write( "\"" );
        }

        /// <summary>
        /// Writes the given number of tab characters to a stream
        /// </summary>
        /// <param name="stream">Stream to write to</param>
        /// <param name="indent">Number of tabs</param>
        private void WriteWhitespace( StreamWriter stream, int indent ) {
            for( int i = 0; i < indent; i++ ) {
                stream.Write( '\t' );
            }
        }

        /// <summary>
        /// Checks whether or not this node has any children
        /// </summary>
        /// <returns>True if an array with no children, false otherwise</returns>
        private bool IsEmpty() {
            if( NodeArray != null ) {
                return NodeArray.Count == 0;
            } else {
                return ( NodeData as string ) == null;
            }
        }
        #endregion

        #region Accessors

        /// <summary>
        /// Gets the node at the given address. May be used to build structure.
        /// </summary>
        /// <param name="args">An ordered list of keys, like a path</param>
        /// <param name="create">If true, will create any nodes it does not find along the path.</param>
        /// <param name="index">Start index of the arg array</param>
        /// <returns>The FileNode at the given location, or null if the location was not found / created</returns>
        public FileNode GetNodeAt( string[] args, bool create = true, int index = 0 ) {
            if( index >= args.Length ) {
                return this;
            }
            if( this.NodeType == ValueType.Array ) {
                Dictionary<String, FileNode> data = (Dictionary<String, FileNode>)NodeData;
                if( ContainsKey( args[index] ) ) {
                    return data[args[index]].GetNodeAt( args, create, index + 1 );
                } else if( create ) {
                    FileNode newNode = new FileNode();
                    data.Add( args[index], newNode );
                    return newNode.GetNodeAt( args, create, index + 1 );
                }
            }
            return null;
        }

        /// <summary>
        /// Checks whether the given key exists within an array-type node
        /// </summary>
        /// <param name="key">The key to look for</param>
        /// <returns>True if the key was found, false otherwise</returns>
        public bool ContainsKey( string key ) {
            if( NodeType != ValueType.Array ) {
                return false;
            }
            return ( (Dictionary<string, FileNode>)NodeData ).ContainsKey( key );
        }

        #endregion

        #region Modifiers
        /// <summary>
        /// Removes the subnode with the given key. Can only be called on array nodes.
        /// </summary>
        /// <param name="key">Key of the subnode to remove</param>
        /// <returns>True if node was removed, false if not found</returns>
        public bool RemoveSubnode( string key ) {
            return NodeArray.Remove( key );
        }

        /// <summary>
        /// Removes any array nodes without any value-type children
        /// </summary>
        public void CleanTree() {
            Dictionary<string, FileNode> nodes = NodeArray;
            if( nodes != null ) {
                string[] keys = nodes.Keys.ToArray<string>();
                foreach( string key in keys ) {
                    nodes[key].CleanTree();
                    if( nodes[key].IsEmpty() ) {
                        NodeArray.Remove( key );
                    }
                }
            }
        }

        #endregion

        #region Saving and loading
        /// <summary>
        /// Loads a FileNode from stream.
        /// </summary>
        /// <param name="stream">Stream to load from</param>
        /// <returns>FileNode representing the contents of the stream.</returns>
        public static FileNode Load( StreamReader stream, bool useFirstAsRoot = false ) {
            FileNode thisLevel = useFirstAsRoot ? null : new FileNode();

            SkipWhitespace( stream );

            while( !stream.EndOfStream ) {

                SkipWhitespace( stream );
                // Get key
                char nextChar = (char)stream.Read();
                string key = null;
                if( stream.EndOfStream || nextChar == '}' ) {
                    break;
                } else if( nextChar == '"' ) {
                    key = GetStringToken( stream );
                } else {
                    throw new ParseException( string.Format( "Unexpected character '{0}' found when expecting key.", nextChar ) );
                }
                SkipWhitespace( stream );

                // Get value
                nextChar = (char)stream.Read();
                FileNode newNode;
                if( nextChar == '"' ) {
                    newNode = new FileNode( GetStringToken( stream ) );
                } else if( nextChar == '{' ) {
                    newNode = Load( stream );
                } else {
                    throw new ParseException( string.Format( "Unexpected character '{0}' found when expecting value.", nextChar ) );
                }

                if( useFirstAsRoot ) {
                    return newNode;
                }

                thisLevel[key] = newNode;
            }
            return thisLevel;
        }

        /// <summary>
        /// Writes this FileNode to a stream
        /// </summary>
        /// <param name="stream">Stream to write to</param>
        /// <param name="indent">Indentation level of each line.</param>
        public void Save( StreamWriter stream, int indent = 0 ) {
            if( NodeType == ValueType.Array ) {
                Dictionary<string, FileNode> data = NodeArray;
                foreach( KeyValuePair<string, FileNode> entry in data ) {
                    if( entry.Value.NodeType == ValueType.Array ) {
                        WriteWhitespace( stream, indent );
                        WriteFormattedString( stream, entry.Key );
                        stream.WriteLine();

                        WriteWhitespace( stream, indent );
                        stream.WriteLine( '{' );

                        entry.Value.Save( stream, indent + 1 );

                        WriteWhitespace( stream, indent );
                        stream.WriteLine( '}' );
                    } else {
                        WriteWhitespace( stream, indent );
                        WriteFormattedString( stream, entry.Key );
                        stream.Write( "\t\t" );

                        WriteFormattedString( stream, entry.Value.NodeData as string );
                        stream.WriteLine();
                    }
                }
            } else {

            }
        }
        #endregion
    }

    public class ParseException : ApplicationException {
        public ParseException() : base() { }
        public ParseException( string message ) : base() { }
    }
}