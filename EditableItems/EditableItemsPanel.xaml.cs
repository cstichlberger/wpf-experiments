using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace EditableItemsExample
{
	/// <summary>
	/// Control holding positioned items
	/// 
	/// An aspect specific to this implementation is the use of a ItemsControl for managing the positioned items.
	/// This necessitates special handling because the elements positioned in the Canvas are the ItemControl's ContentPresenters
	/// 
	/// An attempt to use GongSolution's WPF DragDrop was unsuccessful - problems with the drag position and a complete freeze
	/// of the Window (Win7) UI mouse interactions led to dropping it for now.
	/// </summary>
	public partial class EditableItemsPanel
	{
		EditableItemsPanelHandler dragStateHandler;
		
		public EditableItemsPanel()
		{
			InitializeComponent();
			Loaded += LoadedHandler;
		}
		

		void LoadedHandler(object sender, RoutedEventArgs e)
		{
			dragStateHandler = new EditableItemsPanelHandler((Canvas)VisualTreeUtils.GetItemsPanel(itemsControl), this, (IEditableItemsViewModel)DataContext);
		}
	}
	
	/// <summary>
	/// Selection and positioning logic for EditableItemsPanel
	/// 
	/// Heavily adapted from https://denisvuyka.wordpress.com/2007/10/15/wpf-simple-adorner-usage-with-drag-and-resize-operations/
	/// </summary>
	public class EditableItemsPanelHandler
	{
		Canvas itemsCanvas;
		AdornerLayer adornerLayer;

		IEditableItemsViewModel viewModel;

		//state for selection and drag and drop
		bool isMouseDown;
		bool isDragging;
		bool selected;
		UIElement selectedElement = null;
		
		UIElement SelectedCanvasElement { get { return (UIElement)VisualTreeHelper.GetParent(selectedElement); }}

		System.Windows.Point startPoint;
		private double originalLeft;
		private double originalTop;
		
		public EditableItemsPanelHandler(Canvas canvas, FrameworkElement container, IEditableItemsViewModel viewModel)
		{
			itemsCanvas = canvas;
			this.viewModel = viewModel;
			
			itemsCanvas.PreviewMouseLeftButtonDown += PreviewMouseLeftButtonDown;
			itemsCanvas.PreviewMouseLeftButtonUp += DragFinished;
			
			container.MouseLeftButtonDown += MouseLeftButtonDown;
			container.MouseLeftButtonUp += DragFinished;
			container.MouseMove += MouseMove;
			container.MouseLeave += MouseLeave;
		}
		
		public void MouseLeave(object sender, MouseEventArgs e)
		{
			var position = e.GetPosition(itemsCanvas);
			
			//when hovering over the adorner thumbs, MouseLeave is also triggered - protect against this by checking
			//whether the mouse is actually leaving the window bounds
			int margin = 5;
			if (position.X < margin || position.Y < margin || position.X > itemsCanvas.ActualWidth - margin || position.Y > itemsCanvas.ActualHeight - margin)
			{
				StopDragging();
				e.Handled = true;
			}
		}

		public void DragFinished(object sender, MouseButtonEventArgs e)
		{
			StopDragging();
			e.Handled = true;
		}
		
		private void StopDragging()
		{
			if (isMouseDown)
			{
				isMouseDown = false;
				isDragging = false;
				viewModel.BoundsUpdateListener.BoundsUpdated((EditableItem)((FrameworkElement)selectedElement).DataContext);
			}
		}
		
		public void MouseMove(object sender, MouseEventArgs e)
		{
			if (isMouseDown)
			{
				if ((!isDragging) &&
				    ((Math.Abs(e.GetPosition(itemsCanvas).X - startPoint.X) > SystemParameters.MinimumHorizontalDragDistance) ||
				     (Math.Abs(e.GetPosition(itemsCanvas).Y - startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)))
				{
					isDragging = true;
				}

				if (isDragging)
				{
					System.Windows.Point position = Mouse.GetPosition(itemsCanvas);
					
					var newY = position.Y - (startPoint.Y - originalTop);
					
					Canvas.SetTop(SelectedCanvasElement, newY);
					Canvas.SetLeft(SelectedCanvasElement, position.X - (startPoint.X - originalLeft));
				}
			}
		}
		
		public void MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			Deselect();
		}
		
		public bool Deselect(object newSelection = null)
		{
			if (newSelection != null && newSelection == selectedElement)
			{
				return true;
			}
			if (selected)
			{
				selected = false;
				if (selectedElement != null)
				{
					var adorner = (ResizingAdorner)adornerLayer.GetAdorners(SelectedCanvasElement)[0];
					adorner.DeregisterBoundsChangeHandlers();
					adornerLayer.Remove(adorner);
					Canvas.SetZIndex(SelectedCanvasElement, 0);
					selectedElement = null;
				}
			}
			return false;
		}

		private void ItemMouseDown(MouseButtonEventArgs e)
		{
			isMouseDown = true;
			originalLeft = Canvas.GetLeft(SelectedCanvasElement);
			originalTop = Canvas.GetTop(SelectedCanvasElement);
			startPoint = e.GetPosition(itemsCanvas);
		}
		
		// Handler for element selection on the canvas providing resizing adorner
		public void PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			// Remove selection on clicking anywhere in the container
			bool alreadySelected = Deselect(e.Source);
			if (alreadySelected)
			{
				ItemMouseDown(e);
				e.Handled = true;

				return;
			}

			// If any element except canvas is clicked,
			// assign the selected element and add the adorner
			if (e.Source != itemsCanvas)
			{
				selectedElement = e.Source as UIElement;
				ItemMouseDown(e);
				Canvas.SetZIndex(SelectedCanvasElement, 1);
				adornerLayer = AdornerLayer.GetAdornerLayer(SelectedCanvasElement);
				adornerLayer.Add(new ResizingAdorner(SelectedCanvasElement, viewModel.BoundsValidator, viewModel.BoundsUpdateListener));
				selected = true;
				e.Handled = true;
			}
		}
	}
	
	/// <summary>
	/// Adorner for resizing that also highlights the element in invalid positions
	/// </summary>
	public class ResizingAdorner : Adorner
	{
		// Use Thumbs for visual elements - the Thumbs have built-in mouse input handling
		Thumb topLeft, topRight, bottomLeft, bottomRight;
		System.Windows.Shapes.Rectangle validationOverlay;

		// To store and manage the adorner's visual children.
		VisualCollection visualChildren;
		

		IBoundsValidator boundsValidator;
		IBoundsUpdateListener updateListener;
		
		private FrameworkElement Target { get { return (FrameworkElement)AdornedElement; }}

		public ResizingAdorner(UIElement Target, IBoundsValidator boundsValidator, IBoundsUpdateListener updateListener)
			: base(Target)
		{
			this.boundsValidator = boundsValidator;
			this.updateListener = updateListener;
			
			visualChildren = new VisualCollection(this);

			topLeft = BuildAdornerCorner(Cursors.SizeNWSE, HandleTopLeft);
			topRight = BuildAdornerCorner(Cursors.SizeNESW, HandleTopRight);
			bottomLeft = BuildAdornerCorner(Cursors.SizeNESW, HandleBottomLeft);
			bottomRight = BuildAdornerCorner(Cursors.SizeNWSE, HandleBottomRight);
			
			validationOverlay = new System.Windows.Shapes.Rectangle();
			validationOverlay.Fill = new SolidColorBrush(System.Windows.Media.Colors.Orange);
			validationOverlay.Opacity = 0;
			validationOverlay.IsHitTestVisible = false;
			visualChildren.Add(validationOverlay);
			
			ModifyChangeHandlers(true);
			
			InitializeContentPresenterBinding();
			
			ValidateBounds(Item);
		}
		
		
		public void DeregisterBoundsChangeHandlers()
		{
			ModifyChangeHandlers(false);
		}
		
		void ModifyChangeHandlers(bool add)
		{
			ChangeHandler(Canvas.TopProperty, typeof(Canvas), add);
			ChangeHandler(Canvas.LeftProperty, typeof(Canvas), add);
			ChangeHandler(FrameworkElement.WidthProperty, typeof(FrameworkElement), add);
			ChangeHandler(FrameworkElement.HeightProperty, typeof(FrameworkElement), add);
		}
		
		void ChangeHandler(DependencyProperty property, Type type, bool add)
		{
			var descriptor = DependencyPropertyDescriptor.FromProperty(property, type);
			if (add)
			{
				descriptor.AddValueChanged(Target, ValidateBounds);
			}
			else
			{
				descriptor.RemoveValueChanged(Target, ValidateBounds);
			}
		}
		
		void InitializeContentPresenterBinding()
		{
			//special handling for the Canvas' direct children (ContentPresenter) and the actual element
			//bind the size of the element and it's ContentPresenter together so resizing works as expected
			FrameworkElement child = (FrameworkElement)VisualTreeHelper.GetChild(AdornedElement, 0);
			var binding = new Binding("Width");
			binding.Source = child;
			binding.Mode = BindingMode.TwoWay;
			Target.SetBinding(FrameworkElement.WidthProperty, binding);
			
			binding = new Binding("Height");
			binding.Source = child;
			binding.Mode = BindingMode.TwoWay;
			Target.SetBinding(FrameworkElement.HeightProperty, binding);
		}
		
		Action<object, DragDeltaEventArgs> GenericResize(Action<Thumb, DragDeltaEventArgs> handler)
		{
			return (sender, args) => {
				FrameworkElement Target = this.AdornedElement as FrameworkElement;
				Thumb thumb = sender as Thumb;
				
				if (Target == null || thumb == null) return;
				FrameworkElement parentElement = Target.Parent as FrameworkElement;
				
				// Ensure that Width and Height are properly initialized after the resize.
				EnforceSize(Target);
				handler(thumb, args);
			};
		}

		
		#region Change methods
		
		
		void HandleBottomRight(Thumb thumb, DragDeltaEventArgs args)
		{
			ChangeWidth(thumb, args);
			ChangeHeight(thumb, args);
		}

		void HandleTopRight(Thumb thumb, DragDeltaEventArgs args)
		{
			ChangeTop(thumb, args);
			ChangeWidth(thumb, args);
		}
		
		void HandleTopLeft(Thumb thumb, DragDeltaEventArgs args)
		{
			ChangeLeft(thumb, args);
			ChangeTop(thumb, args);
		}

		void HandleBottomLeft(Thumb thumb, DragDeltaEventArgs args)
		{
			ChangeHeight(thumb, args);
			ChangeLeft(thumb, args);
		}
		
		#endregion
		
		#region Change methods
		
		void ChangeWidth(Thumb thumb, DragDeltaEventArgs args)
		{
			Width = Math.Max(Width + args.HorizontalChange, thumb.DesiredSize.Width);
		}
		
		void ChangeHeight(Thumb thumb, DragDeltaEventArgs args)
		{
			Height = Math.Max(Height + args.VerticalChange, thumb.DesiredSize.Height);
		}
		
		void ChangeTop(Thumb thumb, DragDeltaEventArgs args)
		{
			var oldTop = Top;
			Top = Top + args.VerticalChange;
			//width and height need to be changed in sync with top and left in order to be stable
			Height = Math.Max(Height + (oldTop - Top), thumb.DesiredSize.Height);
		}
		
		void ChangeLeft(Thumb thumb, DragDeltaEventArgs args)
		{
			var oldLeft = Left;
			Left = Left + args.HorizontalChange;
			//width and height need to be changed in sync with top and left in order to be stable
			Width = Math.Max(Width + (oldLeft - Left), thumb.DesiredSize.Width);
		}
		
		#endregion
		
		#region Supporting accessors
		
		new double Width { get {return Target.Width;} set { Target.Width = value; }}
		new double Height { get {return Target.Height;} set { Target.Height = value; }}
		double Top { get {return Canvas.GetTop(Target);} set { Canvas.SetTop(Target, value); }}
		double Left { get {return Canvas.GetLeft(Target);} set { Canvas.SetLeft(Target, value); }}

		void ValidateBounds(object sender, EventArgs args)
		{
			ValidateBounds(Item);
		}
		
		void ValidateBounds(IEditableItem item)
		{
			bool valid = boundsValidator.HasValidBounds(Item);
			validationOverlay.Opacity = valid ? 0 : 0.5;
		}
		
		IEditableItem Item {
			get {
				FrameworkElement child = (FrameworkElement)VisualTreeHelper.GetChild(AdornedElement, 0);
				return (IEditableItem)child.DataContext;
			}
		}
		
		#endregion
		
		// Arrange the Adorners.
		protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize)
		{
			// desiredWidth and desiredHeight are the width and height of the element that's being adorned.
			// These will be used to place the ResizingAdorner at the corners of the adorned element.
			double desiredWidth = AdornedElement.DesiredSize.Width;
			double desiredHeight = AdornedElement.DesiredSize.Height;
			// adornerWidth & adornerHeight are used for placement as well.
			double adornerWidth = this.DesiredSize.Width;
			double adornerHeight = this.DesiredSize.Height;

			topLeft.Arrange(new Rect(-adornerWidth / 2, -adornerHeight / 2, adornerWidth, adornerHeight));
			topRight.Arrange(new Rect(desiredWidth - adornerWidth / 2, -adornerHeight / 2, adornerWidth, adornerHeight));
			bottomLeft.Arrange(new Rect(-adornerWidth / 2, desiredHeight - adornerHeight / 2, adornerWidth, adornerHeight));
			bottomRight.Arrange(new Rect(desiredWidth - adornerWidth / 2, desiredHeight - adornerHeight / 2, adornerWidth, adornerHeight));

			validationOverlay.Arrange(new Rect(0, 0, desiredWidth, desiredHeight));
			
			return finalSize;
		}

		// Helper method to instantiate the corner Thumbs, set the Cursor property,
		// set some appearance properties, and add the elements to the visual tree.
		Thumb BuildAdornerCorner(Cursor customizedCursor, Action<Thumb, DragDeltaEventArgs> cornerChangeHandler)
		{
			var cornerThumb = new Thumb();
			cornerThumb.Cursor = customizedCursor;
			cornerThumb.Height = cornerThumb.Width = 10;
			cornerThumb.Opacity = 0.40;
			cornerThumb.Background = new SolidColorBrush(Colors.MediumBlue);

			visualChildren.Add(cornerThumb);
			
			cornerThumb.DragDelta += new DragDeltaEventHandler(GenericResize(cornerChangeHandler));
			cornerThumb.DragCompleted += new DragCompletedEventHandler(cornerThumb_DragCompleted);
			
			return cornerThumb;
		}

		void cornerThumb_DragCompleted(object sender, DragCompletedEventArgs e)
		{
			updateListener.BoundsUpdated(Item);
		}

		// This method ensures that the Widths and Heights are initialized.  Sizing to content produces
		// Width and Height values of Double.NaN.  Because this Adorner explicitly resizes, the Width and Height
		// need to be set first.  It also sets the maximum size of the adorned element.
		void EnforceSize(FrameworkElement Target)
		{
			if (Target.Width.Equals(Double.NaN))
			{
				Target.Width = Target.DesiredSize.Width;
			}

			if (Target.Height.Equals(Double.NaN))
			{
				Target.Height = Target.DesiredSize.Height;
			}

			FrameworkElement parent = Target.Parent as FrameworkElement;
			if (parent != null)
			{
				Target.MaxHeight = parent.ActualHeight;
				Target.MaxWidth = parent.ActualWidth;
			}
		}
		
		// Override the VisualChildrenCount and GetVisualChild properties to interface with
		// the adorner's visual collection.
		protected override int VisualChildrenCount { get { return visualChildren.Count; } }
		
		protected override Visual GetVisualChild(int index) { return visualChildren[index]; }
	}
}