using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace EditableItemsExample
{
	/// <summary>
	/// ViewModel exposing the editable items
	/// </summary>
	public interface IEditableItemsViewModel
	{
		ObservableCollection<IEditableItem> Items { get; }
		
		IBoundsValidator BoundsValidator { get; }
		
		IBoundsUpdateListener BoundsUpdateListener { get; }
	}
	
	/// <summary>
	/// An editable item from the view perspective
	/// </summary>
	public interface IEditableItem
	{
		IBounds Bounds { get; }
	}
	
	/// <summary>
	/// The bounds of an item
	/// </summary>
	public interface IBounds {
		double Left { get; set; }
		double Top { get; set; }
		double Width { get; set; }
		double Height { get; set; }
	}
	
	/// <summary>
	/// Validates whether an item has valid bounds
	/// </summary>
	public interface IBoundsValidator
	{
		bool HasValidBounds(IEditableItem item);
	}
	
	/// <summary>
	/// Is notified when an item's bounds change definitely (user stops changing the bounds)
	/// </summary>
	public interface IBoundsUpdateListener
	{
		void BoundsUpdated(IEditableItem item);
	}
}
