using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace EditableItemsExample
{
	/// <summary>
	/// IEditableItemsViewModel implementation
	/// </summary>
	public class EditableItemsViewModel: IEditableItemsViewModel, IBoundsValidator, IBoundsUpdateListener
	{
		public ObservableCollection<IEditableItem> Items { get; set; }
		
		public EditableItemsViewModel()
		{
			var items = new ObservableCollection<IEditableItem>();
			
			Random random = new Random();
			
			for (int i = 0; i < 10; i++)
			{
				var item = new EditableItem();
				item.Bounds.Top = random.Next((int)800);
				item.Bounds.Left = random.Next((int)600);
				item.Bounds.Width = random.Next(100) + 10;
				item.Bounds.Height = random.Next(100) + 10;
				items.Add(item);
			}
			Items = items;
		}
		
		public void BoundsUpdated(IEditableItem item)
		{
			System.Diagnostics.Debug.WriteLine("Bounds updated for item " + item);
		}
		
		public bool HasValidBounds(IEditableItem item)
		{
			var itemRect = ToRectangle(item.Bounds);
			foreach (var i in Items)
			{
				if (i != item)
				{
					var rect = ToRectangle(i.Bounds);
					if (itemRect.IntersectsWith(rect))
					{
						return false;
					}
				}
			}
			
			return true;
		}
		
		private System.Drawing.Rectangle ToRectangle(IBounds bounds)
		{
			return new System.Drawing.Rectangle((int)bounds.Left, (int)bounds.Top, (int)bounds.Width, (int)bounds.Height);
		}
		
		public IBoundsValidator BoundsValidator {
			get {
				return this;
			}
		}
		
		public IBoundsUpdateListener BoundsUpdateListener {
			get {
				return this;
			}
		}
	}
	
	/// <summary>
	/// Simple editable item implementation 
	/// </summary>
	public class EditableItem: IEditableItem
	{
		public IBounds Bounds { get; set; }
		
		public EditableItem()
		{
			Bounds = new Bounds();
		}
	}
	
	/// <summary>
	/// Custom bounds/rectangle implemenentation that allows for
	/// updates of the individual values and snaps coordinates and size to a grid,
	/// limiting to a minimum size
	/// </summary>
	public class Bounds: NotifyPropertyChangedBase, IBounds
	{
		private double top, left, width, height;
		
		public double Top { get {return top;} set {SetProperty(ref top, SnapToGrid(value));} }
		public double Left { get {return left; } set {SetProperty(ref left, SnapToGrid(value));} }
		public double Width { get {return width; } set {SetProperty(ref width, SnapToGrid(EnforceMinSize(value)));} }
		public double Height { get {return height; } set {SetProperty(ref height, SnapToGrid(EnforceMinSize(value)));} }
		
		private double SnapToGrid(double coordinate)
		{
			return Math.Round(coordinate / 10.0) * 10;
		}
		
		private double EnforceMinSize(double candidateSize)
		{
			int minSize = 100;
			return Math.Max(candidateSize, minSize);
		}
	}
}