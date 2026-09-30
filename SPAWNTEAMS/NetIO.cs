using System.Reflection;

namespace SpawnTeams.Networking
{
    /// <summary>
    /// NetDataWriter.Put / NetDataReader.Get tienen sobrecargas que usan ReadOnlySpan&lt;T&gt;.
    /// Basta con que ESA sobrecarga exista para que el compilador exija poder resolver el tipo
    /// Span al compilar CUALQUIER llamada a Put/Get, aunque usemos la sobrecarga de string/int.
    /// En algunas instalaciones (System.Memory.dll incompleta o redirigida) ese tipo no se
    /// puede cargar y la compilacion falla con CS0518 sin que tengamos culpa en el codigo.
    ///
    /// La solucion: llamar a Put(string)/Put(int)/GetString()/GetInt() por reflexion. Asi el
    /// compilador nunca necesita ver el grupo de sobrecargas completo, solo el nombre del
    /// metodo, y Span deja de ser un problema.
    /// </summary>
    internal static class NetIO
    {
        private static MethodInfo _putString, _putInt, _getString, _getInt;

        private static void EnsureResolved(object writerOrReader)
        {
            if (_putString != null) return;

            var writerType = writerOrReader.GetType().Assembly.GetType(
                "Fika.Core.Networking.LiteNetLib.Utils.NetDataWriter");
            var readerType = writerOrReader.GetType().Assembly.GetType(
                "Fika.Core.Networking.LiteNetLib.Utils.NetDataReader");

            if (writerType != null)
            {
                _putString = writerType.GetMethod("Put", new[] { typeof(string) });
                _putInt = writerType.GetMethod("Put", new[] { typeof(int) });
            }
            if (readerType != null)
            {
                _getString = readerType.GetMethod("GetString", System.Type.EmptyTypes);
                _getInt = readerType.GetMethod("GetInt", System.Type.EmptyTypes);
            }
        }

        public static void PutString(object writer, string value)
        {
            EnsureResolved(writer);
            _putString?.Invoke(writer, new object[] { value ?? string.Empty });
        }

        public static void PutInt(object writer, int value)
        {
            EnsureResolved(writer);
            _putInt?.Invoke(writer, new object[] { value });
        }

        public static string GetString(object reader)
        {
            EnsureResolved(reader);
            return _getString != null ? (string)_getString.Invoke(reader, null) : string.Empty;
        }

        public static int GetInt(object reader)
        {
            EnsureResolved(reader);
            return _getInt != null ? (int)_getInt.Invoke(reader, null) : 0;
        }
    }
}
