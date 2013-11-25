///
/// WF.Player.Core - A Wherigo Player Core for different platforms.
/// Copyright (C) 2012-2013  Dirk Weltz <web@weltz-online.de>
/// Copyright (C) 2012-2013  Brice Clocher <contact@cybisoft.net>
///
/// This program is free software: you can redistribute it and/or modify
/// it under the terms of the GNU Lesser General Public License as
/// published by the Free Software Foundation, either version 3 of the
/// License, or (at your option) any later version.
/// 
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
/// GNU Lesser General Public License for more details.
/// 
/// You should have received a copy of the GNU Lesser General Public License
/// along with this program.  If not, see <http://www.gnu.org/licenses/>.
///

using System;
using System.Collections.Generic;

namespace WF.Player.Core
{
	/// <summary>
	/// Event arguments for a change in an attribute of a Wherigo object.
	/// </summary>
	public class AttributeChangedEventArgs : ObjectEventArgs<WherigoObject>
	{
		/// <summary>
		/// Gets the name of the attribute that changed.
		/// </summary>
		public string PropertyName { get; private set; }

		internal AttributeChangedEventArgs(Cartridge cart, WherigoObject obj, string prop)
			: base(cart, obj)
		{
			PropertyName = prop;
		}
	}

	/// <summary>
	/// Event arguments for a change in the inventory of a container.
	/// </summary>
	public class InventoryChangedEventArgs : ObjectEventArgs<Thing>
	{
		/// <summary>
		/// Gets the old container for the object, or null if there are none.
		/// </summary>
		public Thing OldContainer { get; private set; }

		/// <summary>
		/// Gets the new container for the object, or null if there are none.
		/// </summary>
		public Thing NewContainer { get; private set; }

		internal InventoryChangedEventArgs(Cartridge cart, Thing obj, Thing fromContainer, Thing toContainer)
			: base(cart, obj)
		{
			OldContainer = fromContainer;
			NewContainer = toContainer;
		}
	}

	/// <summary>
	/// Event arguments for a message from the Wherigo engine to be logged.
	/// </summary>
	public class LogMessageEventArgs : WherigoEventArgs
	{
		/// <summary>
		/// Gets the level of logging of the message.
		/// </summary>
		public LogLevel Level { get; private set; }

		/// <summary>
		/// Gets the message.
		/// </summary>
		public string Message { get; private set; }

		internal LogMessageEventArgs(Cartridge cart, LogLevel level, string message)
			: base(cart)
		{
			Level = level;
			Message = message;
		}
	}

	/// <summary>
	/// Event arguments for a message box.
	/// </summary>
	public class MessageBoxEventArgs : WherigoEventArgs
	{
		/// <summary>
		/// Gets the message box descriptor.
		/// </summary>
		public MessageBox Descriptor { get; private set; }

		internal MessageBoxEventArgs(Cartridge cart, MessageBox descriptor)
			: base(cart)
		{
			Descriptor = descriptor;
		}
	}

	/// <summary>
	/// Event arguments for a wherigo object of a certain type.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class ObjectEventArgs<T> : WherigoEventArgs where T : class
	{
		/// <summary>
		/// Gets the wherigo object that this event is associated to.
		/// </summary>
		public T Object { get; private set; }

		internal ObjectEventArgs(Cartridge cart, T obj)
			: base(cart)
		{
			Object = obj;
		}
	}

	/// <summary>
	/// Event arguments for saving the game.
	/// </summary>
	public class SavingEventArgs : WherigoEventArgs
	{
		/// <summary>
		/// Gets if the Cartridge should be closed - and therefore the game be stopped -
		/// after saving is done.
		/// </summary>
		public bool CloseAfterSave { get; private set; }

		internal SavingEventArgs(Cartridge cart, bool closeAfterSave)
			: base(cart)
		{
			CloseAfterSave = closeAfterSave;
		}
	}

	/// <summary>
	/// Event arguments for a screen.
	/// </summary>
	public class ScreenEventArgs : ObjectEventArgs<UIObject>
	{
		/// <summary>
		/// Gets the kind of screen.
		/// </summary>
		public ScreenType Screen { get; private set; }

		internal ScreenEventArgs(Cartridge cart, ScreenType kind, UIObject obj)
			: base(cart, obj)
		{
			Screen = kind;
		}
	}

	/// <summary>
	/// Event arguments for a status text.
	/// </summary>
	public class StatusTextEventArgs : WherigoEventArgs
	{
		/// <summary>
		/// Gets the status text associated with this event.
		/// </summary>
		public string Text;

		internal StatusTextEventArgs(Cartridge cart, string text)
			: base(cart)
		{
			Text = text;
		}
	}

	/// <summary>
	/// Base class for arguments of a Wherigo-related event.
	/// </summary>
	public class WherigoEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the cartridge entity that changed.
		/// </summary>
		public Cartridge Cartridge { get; private set; }

		internal WherigoEventArgs(Cartridge cart)
		{
			Cartridge = cart;
		}
	}

	/// <summary>
	/// Event arguments for a change in zone states.
	/// </summary>
	public class ZoneStateChangedEventArgs : WherigoEventArgs
	{
		/// <summary>
		/// Gets the list of active zones.
		/// </summary>
		public IEnumerable<Zone> Zones { get; private set; }

		internal ZoneStateChangedEventArgs(Cartridge cart, IEnumerable<Zone> z)
			: base(cart)
		{
			Zones = z;
		}
	}
}
