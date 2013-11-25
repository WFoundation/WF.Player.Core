using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.ObjectModel;

namespace WF.Player.Core
{
    /// <summary>
    /// A generic read-only collection of Wherigo objects.
    /// </summary>
    /// <typeparam name="T">A kind of Wherigo object that this collection contains.</typeparam>
    public class WherigoCollection<T> : ReadOnlyCollection<T> where T : WherigoObject
    {

        #region Constructors

        internal WherigoCollection()
            : base(new List<T>())
        {

        }

        internal WherigoCollection(IList<T> list)
            : base(list)
        {

        }

        #endregion
    }
}
