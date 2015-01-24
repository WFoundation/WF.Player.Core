using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;

namespace WF.Player.Core
{
    /// <summary>
    /// A generic read-only collection of Wherigo objects.
    /// </summary>
    /// <typeparam name="T">A kind of Wherigo object that this collection contains.</typeparam>
    public class WherigoCollection<T> : ReadOnlyCollection<T> where T : WherigoObject
    {
        #region Fields

        private IEnumerable<T> _byDistance;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the elements of this collection ordered by ascending distance 
        /// to player. 
        /// </summary>
        /// <remarks>Elements that do not have a distance to the player
        /// are not included in this enumerable.</remarks>
        public IEnumerable<T> ByDistance
        {
            get
            {
                return _byDistance = _byDistance ?? GetByDistance();
            }
        }

        #endregion

        #region Constructors

        internal WherigoCollection()
            : base(new List<T>())
        {

        }

        internal WherigoCollection(IList<T> list)
            : base(list ?? new List<T>())
        {

        }

        #endregion

        #region Sort by distance

        private List<T> GetByDistance()
        {
            List<T> list = new List<T>();

            // Adds all objects with a specified distance to the player.
            foreach (Thing thing in Items.OfType<Thing>())
            {
                // Gets the distance to the player.
                Distance distToPlayer = GetDistanceToPlayerOrDefault(thing);

                // Only adds the thing to the collection if the distance
                // is not null and it is of type T.
                if (distToPlayer != null && thing is T)
                {
                    list.Add(thing as T);
                }
            }

            // Sorts the list by distance, ascending.
            list.Sort(CompareDistances);

            return list;
        }

        private int CompareDistances(T t1, T t2)
        {
            return ((t1 as Thing).VectorFromPlayer.Distance.CompareTo((t2 as Thing).VectorFromPlayer.Distance));
        }

        private Distance GetDistanceToPlayerOrDefault(Thing t)
        {
            if (t == null)
            {
                return null;
            }

            LocationVector lv = t.VectorFromPlayer;

            if (lv == null || lv.Distance == null)
            {
                return null;
            }

            return lv.Distance;
        }

        #endregion
    }
}
