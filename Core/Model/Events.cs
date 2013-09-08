using System;
using System.Collections.Generic;

namespace WF.Player.Core
{
	/// <summary>
	/// Event arguments for a wherigo object of a certain type.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class ObjectEventArgs<T> : EventArgs where T : class
	{
		/// <summary>
		/// Gets the wherigo object that this event is associated to.
		/// </summary>
		public T Object { get; private set; }

		internal ObjectEventArgs(T obj)
		{
			Object = obj;
		}
	}

	/// <summary>
	/// Event arguments for a change in an attribute of a Wherigo object.
	/// </summary>
	public class AttributeChangedEventArgs : ObjectEventArgs<Table>
	{
		/// <summary>
		/// Gets the name of the attribute that changed.
		/// </summary>
		public string PropertyName { get; private set; }

		internal AttributeChangedEventArgs(Table obj, string prop)
			: base(obj)
		{
			PropertyName = prop;
		}
	}

	/// <summary>
	/// Event arguments for a change in the cartridge entity.
	/// </summary>
	public class CartridgeEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the cartridge entity that changed.
		/// </summary>
		public Cartridge Cartridge { get; private set; }

		internal CartridgeEventArgs(Cartridge cart)
		{
			Cartridge = cart;
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

		internal InventoryChangedEventArgs(Thing obj, Thing fromContainer, Thing toContainer)
			: base(obj)
		{
			OldContainer = fromContainer;
			NewContainer = toContainer;
		}
	}

	/// <summary>
	/// Event arguments for a message from the Wherigo engine to be logged.
	/// </summary>
	public class LogMessageEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the level of logging of the message.
		/// </summary>
		public LogLevel Level { get; private set; }

		/// <summary>
		/// Gets the message.
		/// </summary>
		public string Message { get; private set; }

		internal LogMessageEventArgs(LogLevel level, string message)
		{
			Level = level;
			Message = message;
		}
	}

	/// <summary>
	/// Event arguments for a special command requested by the Wherigo engine.
	/// </summary>
	public class NotifyOSEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the command that is requested.
		/// </summary>
		public string Command { get; private set; }

		internal NotifyOSEventArgs(string c)
		{
			Command = c;
		}
	}

	/// <summary>
	/// Event arguments for a message box.
	/// </summary>
	public class MessageBoxEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the message box descriptor.
		/// </summary>
		public MessageBox Descriptor { get; private set; }

		internal MessageBoxEventArgs(MessageBox descriptor)
		{
			Descriptor = descriptor;
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

		internal ScreenEventArgs(ScreenType kind, UIObject obj)
			: base(obj)
		{
			Screen = kind;
		}
	}

	/// <summary>
	/// Event arguments for a status text.
	/// </summary>
	public class StatusTextEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the status text associated with this event.
		/// </summary>
		public string Text;

		internal StatusTextEventArgs(string text)
		{
			Text = text;
		}
	}

	/// <summary>
	/// Event arguments for a UI synchronisation.
	/// </summary>
	public class SynchronizeEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the action to execute in the UI thread.
		/// </summary>
		public Action Tick { get; private set; }

		internal SynchronizeEventArgs(Action tick)
		{
			Tick = tick;
		}
	}

	/// <summary>
	/// Event arguments for a change in zone states.
	/// </summary>
	public class ZoneStateChangedEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the list of active zones.
		/// </summary>
		public IList<Zone> Zones { get; private set; }

		internal ZoneStateChangedEventArgs(List<Zone> z)
		{
			Zones = z;
		}
	}
}
