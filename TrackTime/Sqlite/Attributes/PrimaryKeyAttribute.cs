using System;
using System.Collections.Generic;
using System.Text;

namespace TrackTime.Sqlite.Attributes;

[AttributeUsage(AttributeTargets.Property)]
internal class PrimaryKeyAttribute : Attribute { }
