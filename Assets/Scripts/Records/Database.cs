using System;
using System.Data;
using System.Data.Common;
using Mono.Data.Sqlite;
using System.Collections;
using System.Collections.Generic;

public class Database: IDisposable {
    private readonly SqliteConnection connection;
    private SqliteTransaction transection;

    public Database(string path) {
        connection = new SqliteConnection(string.Format("URI=file:{0}", path));
        connection.Open();
    }

    public void BeginTransection() {
        if(transection != null) return;
        transection = connection.BeginTransaction();
    }

    public void BeginTransection(IsolationLevel isoLevel) {
        if(transection != null) return;
        transection = connection.BeginTransaction(isoLevel);
    }

    public void CommitTransection() {
        if(transection == null) return;
        transection.Commit();
        transection.Dispose();
        transection = null;
    }

    public void RunSql(string commandText) {
        using(var command = connection.CreateCommand()) {
            command.CommandText = commandText;
            command.ExecuteNonQuery();
        }
    }

    public void RunSql(string commandText, params object[] parameters) {
        using(var command = connection.CreateCommand()) {
            command.CommandText = commandText;
            AppendParameters(command.Parameters, parameters);
            command.ExecuteNonQuery();
        }
    }

    public IEnumerable<IDataRecord> QuerySql(string commandText) {
        using(var command = connection.CreateCommand()) {
            command.CommandText = commandText;
            using(var reader = command.ExecuteReader()) {
                var enumerator = new DbEnumerator(reader, true);
                while(enumerator.MoveNext())
                    yield return enumerator.Current as IDataRecord;
            }
        }
    }

    public IEnumerable<IDataRecord> QuerySql(string commandText, params object[] parameters) {
        using(var command = connection.CreateCommand()) {
            command.CommandText = commandText;
            AppendParameters(command.Parameters, parameters);
            using(var reader = command.ExecuteReader()) {
                var enumerator = new DbEnumerator(reader, true);
                while(enumerator.MoveNext())
                    yield return enumerator.Current as IDataRecord;
            }
        }
    }

    private static void AppendParameters(SqliteParameterCollection collection, object[] parameters) {
        if(parameters == null || parameters.Length < 1) return;
        foreach(var parameter in parameters) {
            SqliteParameter sqliteParam = parameter as SqliteParameter;
            if(sqliteParam != null) {
                collection.Add(sqliteParam);
                continue;
            }
            // Perform Type guessing
            DbType dbType = DbType.String;
            switch(Convert.GetTypeCode(parameter)) {
                case TypeCode.Empty:
                case TypeCode.DBNull:
                    collection.Add(new SqliteParameter(dbType, DBNull.Value));
                    continue;
                case TypeCode.Boolean: dbType = DbType.Boolean; break;
                case TypeCode.Byte: dbType = DbType.Byte; break;
                case TypeCode.SByte: dbType = DbType.SByte; break;
                case TypeCode.UInt16: dbType = DbType.UInt16; break;
                case TypeCode.Int16: dbType = DbType.Int16; break;
                case TypeCode.UInt32: dbType = DbType.UInt32; break;
                case TypeCode.Int32: dbType = DbType.Int32; break;
                case TypeCode.UInt64: dbType = DbType.UInt64; break;
                case TypeCode.Int64: dbType = DbType.Int64; break;
                case TypeCode.Single: dbType = DbType.Single; break;
                case TypeCode.Double: dbType = DbType.Double; break;
                case TypeCode.Decimal: dbType = DbType.Decimal; break;
                case TypeCode.DateTime: dbType = DbType.DateTime; break;
            }
            collection.Add(new SqliteParameter(dbType, parameter));
        }
    }

    public void Dispose() {
        connection.Close();
        connection.Dispose();
    }
}

public static class DataRecord {
    public static bool GetValueAsBoolean(this IDataRecord dataRecord, int index) {
        return Convert.ToBoolean(dataRecord.GetValue(index));
    }
    public static byte GetValueAsByte(this IDataRecord dataRecord, int index) {
        return Convert.ToByte(dataRecord.GetValue(index));
    }
    public static sbyte GetValueAsSByte(this IDataRecord dataRecord, int index) {
        return Convert.ToSByte(dataRecord.GetValue(index));
    }
    public static ushort GetValueAsUInt16(this IDataRecord dataRecord, int index) {
        return Convert.ToUInt16(dataRecord.GetValue(index));
    }
    public static short GetValueAsInt16(this IDataRecord dataRecord, int index) {
        return Convert.ToInt16(dataRecord.GetValue(index));
    }
    public static uint GetValueAsUInt32(this IDataRecord dataRecord, int index) {
        return Convert.ToUInt32(dataRecord.GetValue(index));
    }
    public static int GetValueAsInt32(this IDataRecord dataRecord, int index) {
        return Convert.ToInt32(dataRecord.GetValue(index));
    }
    public static ulong GetValueAsUInt64(this IDataRecord dataRecord, int index) {
        return Convert.ToUInt64(dataRecord.GetValue(index));
    }
    public static long GetValueAsInt64(this IDataRecord dataRecord, int index) {
        return Convert.ToInt64(dataRecord.GetValue(index));
    }
    public static float GetValueAsSingle(this IDataRecord dataRecord, int index) {
        return Convert.ToSingle(dataRecord.GetValue(index));
    }
    public static double GetValueAsDouble(this IDataRecord dataRecord, int index) {
        return Convert.ToDouble(dataRecord.GetValue(index));
    }
    public static char GetValueAsChar(this IDataRecord dataRecord, int index) {
        return Convert.ToChar(dataRecord.GetValue(index));
    }
    public static DateTime GetValueAsDateTime(this IDataRecord dataRecord, int index) {
        return Convert.ToDateTime(dataRecord.GetValue(index));
    }
    public static string GetValueAsString(this IDataRecord dataRecord, int index) {
        return Convert.ToString(dataRecord.GetValue(index));
    }
}
