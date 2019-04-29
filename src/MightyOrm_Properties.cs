﻿using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

using Mighty.Interfaces;
using Mighty.Keys;
using Mighty.Mapping;
using Mighty.Plugins;
using Mighty.Profiling;
using Mighty.Validation;

// <summary>
// `MightyOrm_Propeties.cs` holds various properties which used to be in the various interface files.
// </summary>
// <remarks>
// TO DO: And now could probably go into the main `MightyOrm.cs` file.
// </remarks>
namespace Mighty
{
    public partial class MightyOrm<T> : MightyOrmAbstractInterface<T> where T : class, new()
    {
        #region Npgsql cursor dereferencing
        /// <summary>
        /// Should we auto-dereference cursors when using the Npgsql ADO.NET driver? (See Mighty documentation.)
        /// </summary>
        override public bool NpgsqlAutoDereferenceCursors { get; set; } = true;

        /// <summary>
        /// How many rows at a time should we fetch if auto-dereferencing cursors on the Npgsql ADO.NET driver? (Default value 10,000.) (See Mighty documentation.)
        /// </summary>
        override public int NpgsqlAutoDereferenceFetchSize { get; set; } = 10000;
        #endregion

        #region Properties
        /// <summary>
        /// Connection string
        /// </summary>
        override public string ConnectionString { get; protected set; }

        /// <summary>
        /// ADO.NET provider factory
        /// </summary>
        protected DbProviderFactory Factory { get; set; } // when public, was protected set

        /// <summary>
        /// Plugin
        /// </summary>
        internal PluginBase Plugin { get; set; }

        /// <summary>
        /// Allows setting a global connection string (used by default if nothing else set; set it on untyped <see cref="MightyOrm"/> to set it everywhere).
        /// </summary>
        static public string GlobalConnectionString { get; set; }

        /// <summary>
        /// Allows setting a global validator (used by default if nothing else set; set it on untyped <see cref="MightyOrm"/> to set it everywhere).
        /// </summary>
        static public Validator GlobalValidator { get; set; }

        /// <summary>
        /// Validator
        /// </summary>
        override public Validator Validator { get; protected set; }

        /// <summary>
        /// Allows setting a global sql mapper (used by default if nothing else set; set it on untyped <see cref="MightyOrm"/> to set it everywhere).
        /// </summary>
        static public SqlNamingMapper GlobalSqlMapper { get; set; }

        /// <summary>
        /// C# &lt;=&gt; SQL mapper
        /// </summary>
        override public SqlNamingMapper SqlMapper { get; protected set; }

        /// <summary>
        /// Allows setting a global SQL profiler (used by default if nothing else set; set it on untyped <see cref="MightyOrm"/> to set it everywhere).
        /// </summary>
        static public SqlProfiler GlobalSqlProfiler { get; set; }

        /// <summary>
        /// Optional SQL profiler
        /// </summary>
        override public SqlProfiler SqlProfiler { get; protected set; }

        /// <summary>
        /// Table name (null if non-table-specific instance)
        /// </summary>
        override public string TableName { get; protected set; }

        /// <summary>
        /// Table owner/schema (null if not specified)
        /// </summary>
        override public string TableOwner { get; protected set; }

        /// <summary>
        /// Bare table name (without owner/schema part)
        /// </summary>
        override public string BareTableName { get; protected set; }

        /// <summary>
        /// Keys and sequence
        /// </summary>
        override public PrimaryKeyInfo PrimaryKeys { get; protected set; }

#if KEY_VALUES
        /// <summary>
        /// Column from which value is retrieved by <see cref="KeyValues"/>
        /// </summary>
        override public string ValueField { get; protected set; }
#endif

        /// <summary>
        /// A data contract for the current item type, specified columns and case-sensitivity
        /// </summary>
        override public DataContract DataContract { get; protected set; }

        /// <summary>
        /// true for dynamic instantiation; false if generically typed instantiation
        /// </summary>
        protected bool IsDynamic { get; set; }
#endregion
    }
}
