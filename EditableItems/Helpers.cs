using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EditableItemsExample
{
	/// <summary>
	/// Helper methods for working on the Visual Tree
	/// </summary>
	public static class VisualTreeUtils
	{
		public static Panel GetItemsPanel(DependencyObject itemsControl)
		{
			ItemsPresenter itemsPresenter = GetVisualChild<ItemsPresenter>(itemsControl);
			Panel itemsPanel = VisualTreeHelper.GetChild(itemsPresenter, 0) as Panel;
			return itemsPanel;
		}
		
		public static T GetVisualChild<T>(DependencyObject parent) where T : Visual
		{
			T child = default(T);

			int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
			for (int i = 0; i < numVisuals; i++)
			{
				Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
				child = v as T;
				if (child == null)
				{
					child = GetVisualChild<T>(v);
				}
				if (child != null)
				{
					break;
				}
			}
			return child;
		}
	}
	
	/// <summary>
	/// The venerable INotifyPropertyChanged default implementation
	/// </summary>
	public class NotifyPropertyChangedBase: INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		
		protected virtual void SetProperty<T>(ref T backingField, T newValue, [CallerMemberName] string propertyName = null)
		{
			Debug.Assert(propertyName != null, "The propertyName parameter of SetProperty must be specified when compiling with C# versions lower than 5.0");
			if (propertyName == null)
			{
				throw new ArgumentNullException(propertyName);
			}

			if (!Equals(backingField, newValue))
			{
				backingField = newValue;
				OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
			}
		}

		/// <summary>
		/// Fires the <see cref="PropertyChanged"/> event
		/// </summary>
		/// <param name="args">The arguments to provide to listeners of the event.</param>
		protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
		{
			var handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, args);
			}
		}
	}
}
