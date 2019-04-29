using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
#if NETFRAMEWORK
using System.Transactions;
#endif

using Mighty.ConnectionProviders;
using Mighty.Plugins;
using Mighty.Interfaces;
using Mighty.Mapping;
using Mighty.Parameters;
using Mighty.Profiling;
using Mighty.Validation;

namespace Mighty
{
    /// <summary>
    /// In order to most simply support generics, the dynamic version of Mighty has to be a sub-class of the generic version, but of course the dynamic version is still the nicest version to use! :)
    /// </summary>
    public class MightyOrm : MightyOrm<dynamic>
    {
        #region Constructor
#if KEY_VALUES
        /// <summary>
        /// Constructor for pure dynamic version.
        /// </summary>
        /// <param name="connectionString">
        /// Connection string, with additional Mighty-specific support for non-standard "ProviderName=" property
        /// within the connection string itself.
        /// On .NET Framework (but not .NET Core) this can instead be a connection string name, in which case the
        /// connection string itself and provider name are looked up in the ConnectionStrings section of the .config file.
        /// </param>
        /// <param name="tableName">Table name</param>
        /// <param name="keys">Primary key field name; or comma separated list of names for compound PK</param>
        /// <param name="valueField">Specify the value field, for lookup tables</param>
        /// <param name="sequence">Optional sequence name for PK inserts on sequence-based DBs; or, optionally override
        /// identity retrieval function for identity-based DBs (e.g. specify "@@IDENTITY" here for SQL Server CE). As a special case,
        /// send an empty string (i.e. not the default value of null) to turn off identity support on identity-based DBs.</param>
        /// <param name="columns">Default column list (specifies C# names rather than SQL names, if you have defined a mapper)</param>
        /// <param name="validator">Optional validator</param>
        /// <param name="mapper">Optional C# &lt;-&gt; SQL name mapper</param>
        /// <param name="profiler">Optional SQL profiler</param>
        /// <param name="connectionProvider">Optional connection provider (only needed for providers not yet known to MightyOrm)</param>
        /// <remarks>
        /// What about the SQL Profiler? Should this (really) go into here as a parameter?
        /// ALL column names in the above are pre-mapped C# names, not post-mapped SQL names, where you have a mapper which makes them different.
        /// </remarks>
        public MightyOrm(string connectionString = null,
                         string tableName = null,
                         string keys = null,
                         string valueField = null,
                         string sequence = null,
                         string columns = null,
                         Validator validator = null,
                         SqlNamingMapper mapper = null,
                         SqlProfiler profiler = null,
                         ConnectionProvider connectionProvider = null)
        {
            IsDynamic = true;

            Init(connectionString, tableName, keys,
                valueField,
                sequence, columns, validator, mapper, profiler, connectionProvider);
        }
#else
        /// <summary>
        /// Constructor for pure dynamic version.
        /// </summary>
        /// <param name="connectionString">
        /// Connection string, with additional Mighty-specific support for non-standard "ProviderName=" property
        /// within the connection string itself.
        /// On .NET Framework (but not .NET Core) this can instead be a connection string name, in which case the
        /// connection string itself and provider name are looked up in the ConnectionStrings section of the .config file.
        /// </param>
        /// <param name="tableName">Table name</param>
        /// <param name="keys">Primary key field name; or comma separated list of names for compound PK</param>
        /// <param name="sequence">Optional sequence name for PK inserts on sequence-based DBs; or, optionally override
        /// identity retrieval function for identity-based DBs (e.g. specify "@@IDENTITY" here for SQL Server CE). As a special case,
        /// send an empty string (i.e. not the default value of null) to turn off identity support on identity-based DBs.</param>
        /// <param name="columns">Default column list (specifies C# names rather than SQL names, if you have defined a mapper)</param>
        /// <param name="validator">Optional validator</param>
        /// <param name="mapper">Optional C# &lt;-&gt; SQL name mapper</param>
        /// <param name="profiler">Optional SQL profiler</param>
        /// <param name="connectionProvider">Optional connection provider (only needed for providers not yet known to MightyOrm)</param>
        /// <remarks>
        /// What about the SQL Profiler? Should this (really) go into here as a parameter?
        /// ALL column names in the above are pre-mapped C# names, not post-mapped SQL names, where you have a mapper which makes them different.
        /// </remarks>
        public MightyOrm(string connectionString = null,
                         string tableName = null,
                         string keys = null,
                         string sequence = null,
                         string columns = null,
                         Validator validator = null,
                         SqlNamingMapper mapper = null,
                         SqlProfiler profiler = null,
                         ConnectionProvider connectionProvider = null)
        {
            IsDynamic = true;

            Init(connectionString, tableName, keys,
                sequence, columns, validator, mapper, profiler, connectionProvider);
        }
#endif
        #endregion

        #region Convenience factory
        /// <summary>
        /// Return a new non-table specific instances of <see cref="MightyOrm"/> (equivalent to a constructor call).
        /// </summary>
        /// <param name="connectionString">
        /// Connection string, with additional Mighty-specific support for non-standard "ProviderName=" property
        /// within the connection string itself.
        /// On .NET Framework (but not .NET Core) this can instead be a connection string name, in which case the
        /// connection string itself and provider name are looked up in the ConnectionStrings section of the .config file.
        /// </param>
        /// <returns></returns>
        /// <remarks>
        /// Static, so can't be made part of any kind of interface, even though we want this on the generic and dynamic versions.
        /// I think this requires new because of the conflict with the MightyOrm&lt;T&gt; version.
        /// TO DO: check.
        /// </remarks>
        new static public MightyOrm Open(string connectionString = null)
        {
            return new MightyOrm(connectionString);
        }
#endregion

    }

    /// <summary>
    /// Strongly typed MightyOrm instance.
    /// </summary>
    /// <typeparam name="T">The generic type for items returned by this instance</typeparam>
    public partial class MightyOrm<T> : MightyOrmAbstractInterface<T> where T : class, new()
    {
        #region Constructor
#if KEY_VALUES
        /// <summary>
        /// Strongly typed MightyOrm constructor
        /// </summary>
        /// <param name="connectionString">
        /// Connection string, with additional Mighty-specific support for non-standard "ProviderName=" property
        /// within the connection string itself.
        /// On .NET Framework (but not .NET Core) this can instead be a connection string name, in which case the
        /// connection string itself and provider name are looked up in the ConnectionStrings section of the .config file.
        /// </param>
        /// <param name="tableName">Override the table name (defaults to using T class name)</param>
        /// <param name="keys">Primary key field name; or comma separated list of names for compound PK</param>
        /// <param name="valueField">Specify the value field, for lookup tables</param>
        /// <param name="sequence">Optional sequence name for PK inserts on sequence-based DBs; or, optionally override
        /// identity retrieval function for identity-based DBs (e.g. specify "@@IDENTITY" here for SQL Server CE). As a special case,
        /// send an empty string (i.e. not the default value of null) to turn off identity support on identity-based DBs.</param>
        /// <param name="columns">Default column list (specifies C# names rather than SQL names, if you have defined a mapper)</param>
        /// <param name="validator">Optional validator</param>
        /// <param name="mapper">Optional C# &lt;-&gt; SQL name mapper</param>
        /// <param name="profiler">Optional SQL profiler</param>
        /// <param name="connectionProvider">Optional connection provider (only needed for providers not yet known to MightyOrm)</param>
        public MightyOrm(string connectionString = null,
                         string tableName = null,
                         string keys = null,
                         string valueField = null,
                         string sequence = null,
                         string columns = null,
                         Validator validator = null,
                         SqlNamingMapper mapper = null,
                         SqlProfiler profiler = null,
                         ConnectionProvider connectionProvider = null)
        {
            // If this has been called as part of constructing MightyOrm (non-generic), then return immediately and let that constructor do all the work
            if (this is MightyOrm) return;

            Init(connectionString, tableName, keys,
                valueField,
                sequence, columns, validator, mapper, profiler, connectionProvider);
        }
#else
        /// <summary>
        /// Strongly typed MightyOrm constructor
        /// </summary>
        /// <param name="connectionString">
        /// Connection string, with additional Mighty-specific support for non-standard "ProviderName=" property
        /// within the connection string itself.
        /// On .NET Framework (but not .NET Core) this can instead be a connection string name, in which case the
        /// connection string itself and provider name are looked up in the ConnectionStrings section of the .config file.
        /// </param>
        /// <param name="tableName">Override the table name (defaults to using T class name)</param>
        /// <param name="keys">Primary key field name; or comma separated list of names for compound PK</param>
        /// <param name="sequence">Optional sequence name for PK inserts on sequence-based DBs; or, optionally override
        /// identity retrieval function for identity-based DBs (e.g. specify "@@IDENTITY" here for SQL Server CE). As a special case,
        /// send an empty string (i.e. not the default value of null) to turn off identity support on identity-based DBs.</param>
        /// <param name="columns">Default column list (specifies C# names rather than SQL names, if you have defined a mapper)</param>
        /// <param name="validator">Optional validator</param>
        /// <param name="mapper">Optional C# &lt;-&gt; SQL name mapper</param>
        /// <param name="profiler">Optional SQL profiler</param>
        /// <param name="connectionProvider">Optional connection provider (only needed for providers not yet known to MightyOrm)</param>
        public MightyOrm(string connectionString = null,
                         string tableName = null,
                         string keys = null,
                         string sequence = null,
                         string columns = null,
                         Validator validator = null,
                         SqlNamingMapper mapper = null,
                         SqlProfiler profiler = null,
                         ConnectionProvider connectionProvider = null)
        {
            // If this has been called as part of constructing MightyOrm (non-generic), then return immediately and let that constructor do all the work
            if (this is MightyOrm) return;

            Init(connectionString, tableName, keys,
                sequence, columns, validator, mapper, profiler, connectionProvider);
        }
#endif
        #endregion

        #region Convenience factory
        /// <summary>
        /// Return a new non-table specific instances of <see cref="MightyOrm{T}"/> (equivalent to a constructor call).
        /// </summary>
        /// <param name="connectionString">
        /// Connection string, with additional Mighty-specific support for non-standard "ProviderName=" property
        /// within the connection string itself.
        /// On .NET Framework (but not .NET Core) this can instead be a connection string name, in which case the
        /// connection string itself and provider name are looked up in the ConnectionStrings section of the .config file.
        /// </param>
        /// <returns></returns>
        /// <remarks>Static, so can't be defined anywhere but here.</remarks>
        static public MightyOrm<T> Open(string connectionString = null)
        {
            return new MightyOrm<T>(connectionString);
        }
#endregion

#region Shared initialiser
        // sequence is for sequence-based databases (Oracle, PostgreSQL); there is no default sequence, specify either null or empty string to disable and manually specify your PK values;
        // for non-sequence-based databases, in unusual cases, you may specify this to specify an alternative key retrieval function
        // (e.g. for example to use @@IDENTITY instead of SCOPE_IDENTITY(), in the case of SQL Server CE)
        // keys is a comma separated list; if it has more than one column, you cannot specify sequence or keyRetrievalFunction
        // (if neither sequence nor keyRetrievalFunction are set (which is always the case for compound primary keys), you MUST specify non-null, non-default values for every column in your primary key
        // before saving an object)
        internal void Init(string xconnectionString,
                         string tableName,
                         string keys,
#if KEY_VALUES
                         string valueField,
#endif
                         string sequence,
                         string columns,
                         Validator xvalidator,
                         SqlNamingMapper xmapper,
                         SqlProfiler xprofiler,
                         ConnectionProvider connectionProvider)
        {
            // Slightly hacky, works round the fact that static items are not shared between differently typed classes of the same generic type:
            // use passed-in item; followed by global item for this particular generic class (if specified); followed by global item for the dynamic class
            // (which is intended to be the place you should use, if specifying one of these globally), followed by null object.
            // (A null connectionString still makes sense in .NET Framework, where ConfigFileConnectionProvider will then use the first user connectionString from app.Config)
            string intialConnectionString = xconnectionString ?? GlobalConnectionString ?? MightyOrm.GlobalConnectionString ?? null;
            Validator = xvalidator ?? GlobalValidator ?? MightyOrm.GlobalValidator ?? new NullValidator();
            SqlProfiler = xprofiler ?? GlobalSqlProfiler ?? MightyOrm.GlobalSqlProfiler ?? new NullProfiler();
            SqlMapper = xmapper ?? GlobalSqlMapper ?? MightyOrm.GlobalSqlMapper ?? new NullMapper();

            SetConnection(intialConnectionString, connectionProvider);

            DataContract = DataContractStore.Get(
                IsDynamic, Plugin, Factory, ConnectionString, 
                typeof(T), columns, SqlMapper);

            // typeof(T) if generic; type of user sub-class if dynamic and is sub-class; else null
            Type dataMappingType = GetDataMappingType();
            SetTableNameAndOwner(tableName, dataMappingType);
            PrimaryKeys = new Keys.PrimaryKeyInfo(IsDynamic, DataContract, Plugin, dataMappingType, SqlMapper, keys, sequence);

#if KEY_VALUES
            SetValueField(valueField, dataMappingType);
#endif

#if DYNAMIC_METHODS
            // Add dynamic method support (mainly for compatibility with Massive)
            // TO DO: This line probably shouldn't be here, as it's so intimately tied to code in DynamicMethodProvider
            DynamicObjectWrapper = new DynamicMethodProvider<T>(this);
#endif
        }

        /// <summary>
        /// Sort out the class to use (if any) for this instance of Mighty
        /// </summary>
        /// <returns></returns>
        private Type GetDataMappingType()
        {
            Type dataMappingType = null;

            if (IsDynamic)
            {
                // Subclass-based name overrides for dynamic version of MightyOrm
                dataMappingType = this.GetType();
                // leave tableClass unset if we are not a true sub-class;
                // this test enforces strict sub-class (i.e. does not pass for an instance of the class itself)
                if (!dataMappingType
#if !NETFRAMEWORK
                .GetTypeInfo()
#endif
                .IsSubclassOf(typeof(MightyOrm)))
                {
                    dataMappingType = null;
                }
            }
            else
            {
                // Always use generic class-based name overrides for generic version
                // (We were using the class itself if it was a strict sub-class, but that doesn't make sense for the generic version)
                dataMappingType = typeof(T);
            }

            return dataMappingType;
        }

        private void SetTableNameAndOwner(string tableName, Type dataMappingType)
        {
            if (tableName != null)
            {
                // an empty string can be used to force not using the sub-class name
                TableName = tableName == "" ? null : tableName;
            }
            else if (dataMappingType != null)
            {
                TableName = SqlMapper.GetTableName(dataMappingType);
            }

            if (TableName != null)
            {
                int i = TableName.LastIndexOf('.');
                if (i >= 0)
                {
                    // stays at null otherwise
                    TableOwner = TableName.Substring(0, i);
                }
                BareTableName = TableName.Substring(i + 1);
            }
        }

        private void SetConnection(string connectionString, ConnectionProvider connectionProvider)
        {
            if (connectionProvider == null)
            {
#if NETFRAMEWORK
                // try using the string sent in as a connection string name from the config file; revert to pure connection string if it is not there
                connectionProvider = new ConfigFileConnectionProvider().Init(connectionString);
                if (connectionProvider.ConnectionString == null)
#endif
                {
                    connectionProvider = new PureConnectionStringProvider()
#if NETFRAMEWORK
                        .UsedAfterConfigFile()
#endif
                        .Init(connectionString);
                }
            }
            else
            {
                connectionProvider.Init(connectionString);
            }

            ConnectionString = connectionProvider.ConnectionString;
            Factory = connectionProvider.ProviderFactoryInstance;
            Factory = SqlProfiler.Wrap(Factory);
            Type pluginType = connectionProvider.DatabasePluginType;
            Plugin = (PluginBase)Activator.CreateInstance(pluginType, false);
            Plugin.Mighty = this;
        }

#if KEY_VALUES
        private void SetValueField(string valueField, Type dataMappingType)
        {
            ValueField = string.IsNullOrEmpty(valueField) ? null : SqlMapper.GetColumnName(dataMappingType, valueField);
        }
#endif
        #endregion

        // Only properties with a non-trivial implementation are here, the rest are in the MightyOrm_Properties file.
        #region Properties
        private IEnumerable<dynamic> _TableMetaData;

        /// <summary>
        /// Table meta data (filtered to be only for columns specified by the generic type T, or by consturctor `columns`, if present)
        /// </summary>
        override public IEnumerable<dynamic> TableMetaData
        {
            get
            {
                InitializeTableMetaData();
                return _TableMetaData;
            }
        }
#endregion

#region Thread-safe initializer for table meta-data
        // Thread-safe initialization based on Microsoft DbProviderFactories reference 
        // https://referencesource.microsoft.com/#System.Data/System/Data/Common/DbProviderFactories.cs

        // called within the lock
        private void LoadTableMetaData()
        {
            var sql = Plugin.BuildTableMetaDataQuery(BareTableName, TableOwner);
            IEnumerable<dynamic> unprocessedMetaData;
            dynamic db = this;
            if (!IsDynamic)
            {
                // we need a dynamic query, so on the generic version we create a new dynamic DB object with the same connection info
                db = new MightyOrm(connectionProvider: new PresetsConnectionProvider(ConnectionString, Factory, Plugin.GetType()));
            }
            unprocessedMetaData = (IEnumerable<dynamic>)db.Query(sql, BareTableName, TableOwner);
            var postProcessedMetaData = Plugin.PostProcessTableMetaData(unprocessedMetaData);
            _TableMetaData = FilterTableMetaData(postProcessedMetaData);
        }

        /// <summary>
        /// We drive creating new objects by the table meta-data list, but we only want to add columns which are actually
        /// specified for this instance of Mighty
        /// </summary>
        /// <param name="tableMetaData">The table meta-data</param>
        /// <returns></returns>
        private IEnumerable<dynamic> FilterTableMetaData(IEnumerable<dynamic> tableMetaData)
        {
            foreach (var columnInfo in tableMetaData)
            {
                columnInfo.IS_MIGHTY_COLUMN = DataContract.IsMightyColumn(columnInfo.COLUMN_NAME);
            }
            return tableMetaData;
        }

        // fields for thread-safe initialization of TableMetaData
        // (done once or less per instance of MightyOrm, so not static)
        private ConnectionState _initState; // closed (default value), connecting, open
        private readonly object _lockobj = new object();

        private void InitializeTableMetaData()
        {
            // MS code (re-)uses database connection states
            if (_initState != ConnectionState.Open)
            {
                lock (_lockobj)
                {
                    switch (_initState)
                    {
                        case ConnectionState.Closed:
                            // 'Connecting' state only relevant if the thread which has the lock can recurse back into here
                            // while we are initialising (any other thread can only see Closed or Open)
                            _initState = ConnectionState.Connecting;
                            try
                            {
                                LoadTableMetaData();
                            }
                            finally
                            {
                                // try-finally ensures that even after exception we register that Initialize has been called, and don't keep retrying
                                // (the exception is of course still thrown after the finally code has happened)
                                _initState = ConnectionState.Open;
                            }
                            break;

                        case ConnectionState.Connecting:
                        case ConnectionState.Open:
                            break;

                        default:
                            throw new Exception("unexpected state");
                    }
                }
            }
        }
        #endregion

        // Only methods with a non-trivial implementation are here, the rest are in the MightyOrm_Redirects file.
        #region MircoORM interface
        /// <summary>
        /// Make a new item from the passed-in name-value collection.
        /// </summary>
        /// <param name="nameValues">The name-value collection</param>
        /// <param name="addNonPresentAsDefaults">
        /// If true also include default values for fields not present in the collection
        /// but which exist in columns for the current table in Mighty, which correctly
        /// reflect the defaults of the current database table.
        /// </param>
        /// <returns></returns>
        override public T New(object nameValues = null, bool addNonPresentAsDefaults = true)
        {
            var nvtEnumerator = new NameValueTypeEnumerator(this, nameValues);
            Dictionary<string, object> columnNameToValue = new Dictionary<string, object>();
            foreach (var nvtInfo in nvtEnumerator)
            {
                string columnName = SqlMapper.GetColumnName(typeof(T), nvtInfo.Name);
                DataContractMemberInfo columnInfo;
                if (!DataContract.TryGetDataMemberInfo(columnName, out columnInfo)) continue;
                columnNameToValue.Add(columnName, nvtInfo.Value);
            }
            object item;
            IDictionary<string, object> newItemDictionary = null;
            if (IsDynamic)
            {
                item = new ExpandoObject();
                newItemDictionary = ((ExpandoObject)item).ToDictionary();
            }
            else
            {
                item = new T();
            }
            // drive the loop by the actual column names
            foreach (var columnInfo in TableMetaData)
            {
                if (!columnInfo.IS_MIGHTY_COLUMN) continue;
                string columnName = columnInfo.COLUMN_NAME;
                DataContractMemberInfo member = null;
                if (!IsDynamic) member = DataContract.GetDataMemberInfo(columnName);
                object value;
                if (!columnNameToValue.TryGetValue(columnName, out value))
                {
                    if (!addNonPresentAsDefaults) continue;
                    value = Plugin.GetColumnDefault(columnInfo);
                }
                if (value != null)
                {
                    if (member != null) member.SetValue(item, value);
                    else newItemDictionary.Add(columnName, value);
                }
            }
            return (T)item;
        }

        /// <summary>
        /// Get the meta-data for a single column
        /// </summary>
        /// <param name="column">Column name</param>
        /// <param name="ExceptionOnAbsent">If true throw an exception if there is no such column, otherwise return null.</param>
        /// <returns></returns>
        override public dynamic GetColumnInfo(string column, bool ExceptionOnAbsent = true)
        {
            var info = TableMetaData.Where(c => column.Equals(c.COLUMN_NAME, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (ExceptionOnAbsent && info == null)
            {
                throw new InvalidOperationException("Cannot find table info for column name " + column);
            }
            return info;
        }

        /// <summary>
        /// Get the default value for a column.
        /// </summary>
        /// <param name="columnName">The column name</param>
        /// <returns></returns>
        /// <remarks>
        /// Although it might look more efficient, GetColumnDefault should not do buffering, as we don't
        /// want to pass out the same actual object more than once.
        /// </remarks>
        override public object GetColumnDefault(string columnName)
        {
            var columnInfo = GetColumnInfo(columnName);
            return Plugin.GetColumnDefault(columnInfo);
        }

#if KEY_VALUES
        /// <summary>
        /// Return value column, raising an exception if not specified.
        /// </summary>
        /// <returns></returns>
        string CheckGetValueField(string message)
        {
            if (string.IsNullOrEmpty(ValueField))
            {
                throw new InvalidOperationException(message);
            }
            return ValueField;
        }
#endif

        /// <summary>
        /// Return current table name, raising an exception if there isn't one.
        /// </summary>
        /// <returns></returns>
        protected string CheckGetTableName()
        {
            if (string.IsNullOrEmpty(TableName))
            {
                throw new InvalidOperationException("No table name has been specified");
            }
            return TableName;
        }

        /// <summary>
        /// Checks that every item in the list is valid for the action to be undertaken.
        /// Normally you should not need to override this, but override <see cref="Validator.ValidateForAction"/>
        /// or <see cref="Validator.Validate"/> instead.
        /// </summary>
        /// <param name="action">The ORM action</param>
        /// <param name="items">The list of items. (Can be T, dynamic, or anything else with suitable name-value (and optional type) data in it.)</param>
        virtual internal void ValidateAction(IEnumerable<object> items, OrmAction action)
        {
            if (Validator.Prevalidation == PrevalidationType.Off)
            {
                return;
            }
            // Intention of non-shared error list is thread safety
            List<object> Errors = new List<object>();
            bool valid = true;
            foreach (var item in items)
            {
                int oldCount = Errors.Count;
                Validator.ValidateForAction(action, item, o => Errors.Add(o));
                if (Errors.Count > oldCount)
                {
                    valid = false;
                    if (Validator.Prevalidation == PrevalidationType.Lazy) break;
                }
            }
            if (valid == false || Errors.Count > 0)
            {
                throw new ValidationException(Errors, "Prevalidation failed for one or more items for " + action);
            }
        }

        /// <summary>
        /// Is the passed in item valid against the current validator?
        /// </summary>
        /// <param name="item">The item</param>
        /// <param name="action">Optional action type (defaults to <see cref="OrmAction.Save"/>)</param>
        /// <returns></returns>
        override public List<object> IsValid(object item, OrmAction action = OrmAction.Save)
        {
            List<object> Errors = new List<object>();
            if (Validator != null)
            {
                Validator.ValidateForAction(action, item, o => Errors.Add(o));
            }
            return Errors;
        }

        /// <summary>
        /// True only if the passed in object contains (a) field(s) matching the named primary key(s) of the current table.
        /// </summary>
        /// <param name="item">Item to check</param>
        /// <returns></returns>
        override public bool HasPrimaryKey(object item)
        {
            int count = 0;
            foreach (var info in new NameValueTypeEnumerator(this, item))
            {
                if (PrimaryKeys.IsKey(info.Name)) count++;
            }
            return count == PrimaryKeys.Count;
        }

        /// <summary>
        /// Return primary key for item, as a single object for simple PK, or as object[] for compound PK.
        /// </summary>
        /// <param name="item">The item</param>
        /// <param name="alwaysArray">If true return object[] of 1 item, even for simple PK</param>
        /// <returns></returns>
        override public object GetPrimaryKey(object item, bool alwaysArray = false)
        {
            var pks = new ExpandoObject();
            var pkDictionary = pks.ToDictionary();
            foreach (var info in new NameValueTypeEnumerator(this, item))
            {
                string canonicalKeyName;
                if (PrimaryKeys.IsKey(info.Name, out canonicalKeyName)) pkDictionary.Add(canonicalKeyName, info.Value);
            }
            if (pkDictionary.Count != PrimaryKeys.Count)
            {
                throw new InvalidOperationException("PK field(s) not present in object");
            }
            // re-arrange to specified order
            var retval = new List<object>();
            foreach (var key in PrimaryKeys.Keys)
            {
                retval.Add(pkDictionary[key]);
            }
            var array = retval.ToArray();
            if (array.Length == 1 && !alwaysArray)
            {
                return array[0];
            }
            return array;
        }
#endregion

        // Only methods with a non-trivial implementation are here, the rest are in the DataAccessWrapper abstract class.
#region DataAccessWrapper interface
        /// <summary>
        /// Create command, setting any provider specific features which we assume elsewhere.
        /// </summary>
        /// <param name="sql">The command SQL</param>
        /// <returns></returns>
        internal DbCommand CreateCommand(string sql)
        {
            var command = Factory.CreateCommand();
            command = SqlProfiler.Wrap(command);
            Plugin.SetProviderSpecificCommandProperties(command);
            command.CommandText = sql;
            return command;
        }

        /// <summary>
        /// Create a general-purpose <see cref="DbCommand"/> with named parameters ready for use with Mighty.
        /// Manually creating commands is an advanced use-case; standard Mighty methods create and dispose
        /// of required <see cref="DbCommand"/> and <see cref="DbConnection"/> objects for you.
        /// You should use one of the variants of <see cref="CreateCommand(string, object[])"/>
        /// for all commands passed in to Mighty, since on some providers this sets provider specific properties which are needed to ensure expected behaviour with Mighty.
        /// </summary>
        /// <param name="sql">The command SQL</param>
        /// <param name="inParams">Named input parameters</param>
        /// <param name="outParams">Named output parameters</param>
        /// <param name="ioParams">Named input-output parameters</param>
        /// <param name="returnParams">Named return parameters</param>
        /// <param name="isProcedure">Is the SQL a stored procedure name (with optional argument spec) only?</param>
        /// <param name="connection">The connection to use</param>
        /// <param name="args">Auto-numbered parameters for the SQL</param>
        /// <returns></returns>
        override public DbCommand CreateCommandWithParams(string sql,
            object inParams = null, object outParams = null, object ioParams = null, object returnParams = null, bool isProcedure = false,
            DbConnection connection = null,
            params object[] args)
        {
            return CreateCommandWithParamsAndRowCountCheck(sql,
                inParams, outParams, ioParams, returnParams, isProcedure,
                connection,
                args).Item1;
        }

// We are getting "IDE0059 Value assigned to 'hasRowCountParams' is never used" for this method
// (at least in VS 2019 preview), but this is simply wrong; the value is returned and used.
#pragma warning disable IDE0059 // Value assigned is never used
        /// <summary>
        /// Create command with named, typed, directional parameters.
        /// </summary>
        protected Tuple<DbCommand, bool> CreateCommandWithParamsAndRowCountCheck(string sql,
            object inParams = null, object outParams = null, object ioParams = null, object returnParams = null, bool isProcedure = false,
            DbConnection connection = null,
            params object[] args)
        {
            bool hasRowCountParams = false;
            var command = CreateCommand(sql);
            command.Connection = connection;
            if (isProcedure) command.CommandType = CommandType.StoredProcedure;
            AddParams(command, args);
            AddNamedParams(command, inParams, ParameterDirection.Input);
            hasRowCountParams = AddNamedParams(command, outParams, ParameterDirection.Output);
            AddNamedParams(command, ioParams, ParameterDirection.InputOutput);
            AddNamedParams(command, returnParams, ParameterDirection.ReturnValue);
            return new Tuple<DbCommand, bool>(command, hasRowCountParams);
        }
#pragma warning restore IDE0059

        /// <summary>
        /// Put all output and return parameter values into an expando.
        /// Due to ADO.NET limitations, should only be called after disposing of any associated reader.
        /// </summary>
        /// <param name="cmd">The command</param>
        /// <returns></returns>
        override public dynamic ResultsAsExpando(DbCommand cmd)
        {
            var e = new ExpandoObject();
            var resultDictionary = e.ToDictionary();
            for (int i = 0; i < cmd.Parameters.Count; i++)
            {
                var param = cmd.Parameters[i];
                if (param.Direction != ParameterDirection.Input)
                {
                    var name = Plugin.DeprefixParameterName(param.ParameterName, cmd);
                    var value = Plugin.GetValue(param);
                    resultDictionary.Add(name, value == DBNull.Value ? null : value);
                }
            }
            return e;
        }

        /// <summary>
        /// Add Execute results for <see cref="RowCount"/> parameters.
        /// </summary>
        /// <param name="rowCount">The row count</param>
        /// <param name="outParams">The list of output parameters</param>
        /// <param name="results">The results object to add to</param>
        protected void AppendRowCountResults(int rowCount, object outParams, dynamic results)
        {
            var dictionary = ((ExpandoObject)results).ToDictionary();
            foreach (var paramInfo in new NameValueTypeEnumerator(this, outParams, ParameterDirection.Input))
            {
                if (paramInfo.Value is RowCount)
                {
                    dictionary.Add(paramInfo.Name, rowCount);
                }
            }
        }
        #endregion

        #region ORM actions
        /// <summary>
        /// Create update command
        /// </summary>
        /// <param name="item">The item which contains the update values</param>
        /// <param name="updateNameValuePairs">The columns to update (with values as SQL params)</param>
        /// <param name="whereNameValuePairs">The columns which specify what to update (with values as SQL params)</param>
        /// <returns></returns>
        private DbCommand CreateUpdateCommand(object item, List<string> updateNameValuePairs, List<string> whereNameValuePairs)
        {
            string sql = Plugin.BuildUpdate(TableName, string.Join(", ", updateNameValuePairs), string.Join(" AND ", whereNameValuePairs));
            return CreateCommandWithParams(sql, inParams: item);
        }

        /// <summary>
        /// Create insert command
        /// </summary>
        /// <param name="item">The item containing the update values</param>
        /// <param name="insertNames">The names of the columns to update</param>
        /// <param name="insertValues">The values (as SQL parameters) of the columns to update</param>
        /// <param name="pkFilter">The PK filter setting</param>
        /// <returns></returns>
        private DbCommand CreateInsertCommand(object item, List<string> insertNames, List<string> insertValues, PkFilter pkFilter)
        {
            string sql = Plugin.BuildInsert(TableName, string.Join(", ", insertNames), string.Join(", ", insertValues));
            if (PrimaryKeys.SequenceNameOrIdentityFunction != null)
            {
                sql += ";\r\n" +
                       (Plugin.IsSequenceBased ? Plugin.BuildCurrvalSelect(PrimaryKeys.SequenceNameOrIdentityFunction) : string.Format("SELECT {0}", PrimaryKeys.SequenceNameOrIdentityFunction)) +
                       ";";
            }
            var command = CreateCommand(sql);
            AddNamedParams(command, item, pkFilter: pkFilter);
            if (PrimaryKeys.SequenceNameOrIdentityFunction != null)
            {
                Plugin.FixupInsertCommand(command);
            }
            return command;
        }

        /// <summary>
        /// Create delete command
        /// </summary>
        /// <param name="item">The item containg the param values</param>
        /// <param name="whereNameValuePairs">The column names (and values as SQL params) specifying what to delete</param>
        /// <returns></returns>
        private DbCommand CreateDeleteCommand(object item, List<string> whereNameValuePairs)
        {
            string sql = Plugin.BuildDelete(TableName, string.Join(" AND ", whereNameValuePairs));
            var command = CreateCommand(sql);
            AddNamedParams(command, item, pkFilter: PkFilter.KeysOnly);
            return command;
        }

        /// <summary>
        /// Create the <see cref="DbCommand"/> for an ORM action on an item
        /// </summary>
        /// <param name="originalAction">The action</param>
        /// <param name="item">The item</param>
        /// <param name="revisedAction">The original action unless that was <see cref="OrmAction.Save"/>, in which case this will become <see cref="OrmAction.Insert"/> or <see cref="OrmAction.Update"/></param>
        /// <returns></returns>
        private DbCommand CreateActionCommand(OrmAction originalAction, object item, out OrmAction revisedAction)
        {
            DbCommand command;

            int nKeys = 0;
            int nDefaultKeyValues = 0;

            List<string> insertNames = null;
            List<string> insertValues = null; // param names only, the params pass the actual values
            List<string> updateNameValuePairs = null;
            List<string> whereNameValuePairs = null;

            if (originalAction == OrmAction.Insert || originalAction == OrmAction.Save)
            {
                insertNames = new List<string>();
                insertValues = new List<string>();
            }
            if (originalAction == OrmAction.Update || originalAction == OrmAction.Save)
            {
                updateNameValuePairs = new List<string>();
            }
            if (originalAction == OrmAction.Delete || originalAction == OrmAction.Update || originalAction == OrmAction.Save)
            {
                whereNameValuePairs = new List<string>();
            }

            var argsItem = new ExpandoObject();
            var argsItemDict = argsItem.ToDictionary();
            var count = 0;

            foreach (var nvt in new NameValueTypeEnumerator(this, item, action: originalAction))
            {
                var name = nvt.Name;
                if (name == string.Empty)
                {
                    name = PrimaryKeys.CheckGetKeyName(count, "Too many values trying to map value-only object to primary key list");
                }
                var value = nvt.Value;
                string paramName;
                if (value == null)
                {
                    // Sending NULL in the SQL and not in a param is required to support obscure cases (such as the SQL Server IMAGE type)
                    // where the column refuses to cast from the default VARCHAR NULL param which is created when a parameter is null.
                    paramName = "NULL";
                }
                else
                {
                    paramName = Plugin.PrefixParameterName(name);
                    argsItemDict.Add(name, value);
                }
                if (nvt.Name == null || PrimaryKeys.IsKey(name))
                {
                    nKeys++;
                    if (value == null || value == nvt.Type.GetDefaultValue())
                    {
                        nDefaultKeyValues++;
                    }

                    if (originalAction == OrmAction.Insert || originalAction == OrmAction.Save)
                    {
                        if (PrimaryKeys.SequenceNameOrIdentityFunction == null)
                        {
                            insertNames.Add(name);
                            insertValues.Add(paramName);
                        }
                        else
                        {
                            if (Plugin.IsSequenceBased)
                            {
                                insertNames.Add(name);
                                insertValues.Add(string.Format(Plugin.BuildNextval(PrimaryKeys.SequenceNameOrIdentityFunction)));
                            }
                        }
                    }

                    if (originalAction == OrmAction.Delete || originalAction == OrmAction.Update || originalAction == OrmAction.Save)
                    {
                        whereNameValuePairs.Add(string.Format("{0} = {1}", name, paramName));
                    }
                }
                else
                {
                    if (originalAction == OrmAction.Insert || originalAction == OrmAction.Save)
                    {
                        insertNames.Add(name);
                        insertValues.Add(paramName);
                    }

                    if (originalAction == OrmAction.Update || originalAction == OrmAction.Save)
                    {
                        updateNameValuePairs.Add(string.Format("{0} = {1}", name, paramName));
                    }
                }
                count++;
            }

            if (nKeys > 0)
            {
                if (nKeys != PrimaryKeys.Count)
                {
                    throw new InvalidOperationException("All or no primary key fields must be present in item for " + originalAction);
                }
                if (nDefaultKeyValues > 0 && nDefaultKeyValues != nKeys)
                {
                    throw new InvalidOperationException("All or no primary key fields must start with their default values in item for " + originalAction);
                }
            }
            if (originalAction == OrmAction.Save)
            {
                if (nKeys > 0 && nDefaultKeyValues == 0)
                {
                    revisedAction = OrmAction.Update;
                }
                else
                {
                    revisedAction = OrmAction.Insert;
                }
            }
            else
            {
                revisedAction = originalAction;
            }

            switch (revisedAction)
            {
                case OrmAction.Update:
                    command = CreateUpdateCommand(argsItem, updateNameValuePairs, whereNameValuePairs);
                    break;

                case OrmAction.Insert:
                    if (PrimaryKeys.SequenceNameOrIdentityFunction != null && Plugin.IsSequenceBased)
                    {
                        // our copy of SequenceNameOrIdentityFunction is only ever non-null when there is a non-compound PK
                        insertNames.Add(PrimaryKeys.FieldNames);
                        // TO DO: Should there be two places for BuildNextval? (See above.) Why?
                        insertValues.Add(Plugin.BuildNextval(PrimaryKeys.SequenceNameOrIdentityFunction));
                    }
                    // TO DO: Hang on, we've got a different check here from SequenceNameOrIdentityFunction != null;
                    // either one or other is right, or else some exceptions should be thrown if they come apart.
                    command = CreateInsertCommand(argsItem, insertNames, insertValues, nDefaultKeyValues > 0 ? PkFilter.NoKeys : PkFilter.DoNotFilter);
                    break;

                case OrmAction.Delete:
                    command = CreateDeleteCommand(argsItem, whereNameValuePairs);
                    break;

                default:
                    // use 'Exception' for strictly internal/should not happen/our fault exceptions
                    throw new Exception("incorrect " + nameof(OrmAction) + "=" + originalAction + " at action choice in " + nameof(ActionOnItem));
            }

            return command;
        }

        /// <summary>
        /// Write new PK value into item.
        /// The PK field is a) created if not present and b) filled with the new PK value, where this is possible
        /// (e.g. fields can't be created on POCOs, and property values can't be set on immutable items such as
        /// anonymously typed objects).
        /// </summary>
        /// <param name="item">The item to modify</param>
        /// <param name="pk">The PK value (PK may be int or long depending on the current database)</param>
        /// <param name="createIfNeeded">Writing back the value to a mutable item is worth it, but creating a
        /// replacement item for an immutable item other than the first, on a true insert, isn't</param>
        /// <returns>The modified item</returns>
        private object UpsertItemPK(object item, object pk, bool createIfNeeded)
        {
            var itemAsExpando = item as ExpandoObject;
            if (itemAsExpando != null)
            {
                var dict = itemAsExpando.ToDictionary();
                dict[PrimaryKeys.FieldNames] = pk;
                return item;
            }
            var nvc = item as NameValueCollection;
            if (nvc != null)
            {
                nvc[PrimaryKeys.FieldNames] = pk.ToString();
                return item;
            }
            // Write field back to POCO of type T
            if (IsManagedGenericType(item) && PrimaryKeys.PrimaryKeyDataMember != null)
            {
                PrimaryKeys.PrimaryKeyDataMember.SetValue(item, pk);
                return item;
            }
            if (createIfNeeded)
            {
                // Convert POCO to expando
                var result = item.ToExpando();
                var dict = result.ToDictionary();
                dict[PrimaryKeys.FieldNames] = pk;
                return result;
            }
            return null;
        }

        /// <summary>
        /// Returns true if this is an instance or sublass of <see cref="MightyOrm{T}"/> and <paramref name="item"/> is of type T.
        /// </summary>
        /// <param name="item">The object to check</param>
        /// <returns></returns>
        protected internal bool IsManagedGenericType(object item)
        {
            return !IsDynamic && item is T;
        }
        #endregion

        #region Parameters
        /// <summary>
        /// Add a parameter to a command
        /// </summary>
        /// <param name="cmd">The command</param>
        /// <param name="value">The value</param>
        /// <param name="name">Optional parameter name</param>
        /// <param name="direction">Optional parameter direction</param>
        /// <param name="type">Optional parameter type (for typed NULL support)</param>
        internal void AddParam(DbCommand cmd, object value, string name = null, ParameterDirection direction = ParameterDirection.Input, Type type = null)
        {
            var p = cmd.CreateParameter();
            if (name == string.Empty)
            {
                if (!Plugin.SetAnonymousParameter(p))
                {
                    throw new InvalidOperationException("Current ADO.NET provider does not support anonymous parameters");
                }
            }
            else
            {
                p.ParameterName = Plugin.PrefixParameterName(name ?? cmd.Parameters.Count.ToString(), cmd);
            }
            Plugin.SetDirection(p, direction);
            if (value == null)
            {
                if (type != null)
                {
                    Plugin.SetValue(p, type.GetDefaultValue());
                    // explicitly lock type and size to the values which ADO.NET has just implicitly assigned
                    // (when only implictly assigned, setting Value to DBNull.Value later on causes these to reset, in at least the Npgsql and SQL Server providers)
                    p.DbType = p.DbType;
                    p.Size = p.Size;
                }
                // Some ADO.NET providers completely ignore the parameter DbType when deciding on the .NET type for return values, others do not
                else if (direction != ParameterDirection.Input && !Plugin.IgnoresOutputTypes(p))
                {
                    throw new InvalidOperationException("Parameter \"" + p.ParameterName + "\" - on this ADO.NET provider all output, input-output and return parameters require non-null value or fully typed property, to allow correct SQL parameter type to be inferred");
                }
                p.Value = DBNull.Value;
            }
            else
            {
                var cursor = value as Cursor;
                if (cursor != null)
                {
                    // Placeholder cursor ref; we only need the value if passing in a cursor by value
                    // doesn't work on Postgres.
                    if (!Plugin.SetCursor(p, cursor.Value))
                    {
                        throw new InvalidOperationException("ADO.NET provider does not support cursors");
                    }
                }
                else
                {
                    // Note - the passed in parameter value can be a real cursor ref, this works - at least in Oracle
                    Plugin.SetValue(p, value);
                }
            }
            cmd.Parameters.Add(p);
        }

        /// <summary>
        /// Add auto-named parameters to a command from an array of parameter values (which typically would have been
        /// passed in to Mighty using C# parameter syntax)
        /// </summary>
        /// <param name="cmd">The command</param>
        /// <param name="args">Auto-numbered input parameters</param>
        internal void AddParams(DbCommand cmd, params object[] args)
        {
            if (args == null)
            {
                return;
            }
            foreach (var value in args)
            {
                AddParam(cmd, value);
            }
        }

        /// <summary>
        /// Optionally control whether to add only the PKs or only not the PKs, when creating parameters from object
        /// </summary>
        internal enum PkFilter
        {
            DoNotFilter,
            KeysOnly,
            NoKeys
        }

        /// <summary>
        /// Add named, typed directional params to DbCommand.
        /// </summary>
        /// <param name="cmd">The command</param>
        /// <param name="nameValuePairs">Parameters to add (POCO, anonymous type, NameValueCollection, ExpandoObject, etc.)</param>
        /// <param name="direction">Parameter direction</param>
        /// <param name="pkFilter">Optional PK filter control</param>
        internal bool AddNamedParams(DbCommand cmd, object nameValuePairs, ParameterDirection direction = ParameterDirection.Input, PkFilter pkFilter = PkFilter.DoNotFilter)
        {
            if (nameValuePairs == null)
            {
                // We want to return quickly in this case
                return false;
            }
            bool containsRowCount = false;
            foreach (var paramInfo in new NameValueTypeEnumerator(this, nameValuePairs, direction))
            {
                if (pkFilter == PkFilter.DoNotFilter || (PrimaryKeys.IsKey(paramInfo.Name) == (pkFilter == PkFilter.KeysOnly)))
                {
                    if (paramInfo.Value is RowCount)
                    {
                        if (direction != ParameterDirection.Output)
                        {
                            throw new InvalidOperationException($"${direction} ${nameof(RowCount)} parameter ${paramInfo.Name} is invalid, ${nameof(RowCount)} can only be used for ${ParameterDirection.Output} direction parameters");
                        }
                        containsRowCount = true;
                    }
                    else
                    {
                        AddParam(cmd, paramInfo.Value, paramInfo.Name, direction, paramInfo.Type);
                    }
                }
            }
            return containsRowCount;
        }

        /// <summary>
        /// Produce WHERE clause and inParams or args from either name-value collection or primary key value-only collection
        /// </summary>
        /// <param name="whereParams">Name-value or value-only params</param>
        /// <returns>WHERE; inParams; args</returns>
        internal Tuple<string, object, object[]> GetWhereSpecFromWhereParams(object whereParams)
        {
            var wherePredicates = new List<string>();
            var nameValueArgs = new ExpandoObject();
            var nameValueDictionary = nameValueArgs.ToDictionary();

            var enumerator = new NameValueTypeEnumerator(this, whereParams);

            // If no value names in the whereParams, map the values to the primary key(s)
            if (!enumerator.HasNames())
            {
                return new Tuple<string, object, object[]>(PrimaryKeys.WhereForKeys(), null, PrimaryKeys.KeyValuesFromKey(whereParams));
            }

            // Use (mapped) names as column names and values as values
            foreach (var paramInfo in enumerator)
            {
                string name = SqlMapper.GetColumnName(typeof(T), paramInfo.Name);
                wherePredicates.Add(string.Format("{0} = {1}", name, Plugin.PrefixParameterName(name)));
                nameValueDictionary.Add(name, paramInfo.Value);
            }

            var whereClause = string.Empty;
            if (wherePredicates.Count > 0)
            {
                whereClause = " WHERE " + string.Join(" AND ", wherePredicates);
            }

            return new Tuple<string, object, object[]>(whereClause, nameValueArgs, null);
        }
        #endregion
    }
}