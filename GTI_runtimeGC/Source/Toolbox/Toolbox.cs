using System;

namespace Toolbox;

// TODO(GTI): BROKEN STUB — do NOT use. CleanModMetaData() throws and Cleaned is
// never set. The decompile left this incomplete; the real, working implementation
// lives in ModMetaDataCleaner. FloatMenuUtil used to point its "Clean ModMetaData"
// option here (it threw NotImplementedException at runtime) — that is now fixed to
// call ModMetaDataCleaner instead. This class is currently referenced by nothing;
// kept only to preserve the decompiled layout. Safe to delete.
internal class Toolbox
{
	public static bool Cleaned { get; internal set; }

	internal static void CleanModMetaData()
	{
		throw new NotImplementedException();
	}
}
