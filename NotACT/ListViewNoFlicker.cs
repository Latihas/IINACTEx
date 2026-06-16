using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;
using static System.ComponentModel.DesignerSerializationVisibility;

namespace Advanced_Combat_Tracker;

[ToolboxBitmap(typeof(ListView))]
[SuppressMessage("ReSharper", "UnusedType.Global")]
public class ListViewNoFlicker : ListView {
	private bool updating;
	private bool antiFlicker;
	private int updateCount;
	// private LVS_EX styles;

	[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
	[DesignerSerializationVisibility(Hidden)]
	public bool CustomGridLines { get; set; } = true;

	public ListViewNoFlicker() {
		DrawColumnHeader += ListViewNoFlicker_DrawColumnHeader;
		DrawSubItem += ListViewNoFlicker_DrawSubItem;
		DrawItem += ListViewNoFlicker_DrawItem;
		OwnerDraw = true;
	}

	private static void ListViewNoFlicker_DrawItem(object? _, DrawListViewItemEventArgs e) => e.DrawDefault = true;

	private static void ListViewNoFlicker_DrawSubItem(object? _, DrawListViewSubItemEventArgs e) {
		try {
			if (e.Bounds.Width > 0) {
				Color color;
				if (e.Item.Selected) {
					e.Graphics.FillRectangle(new SolidBrush(SystemColors.Highlight), e.Bounds);
					color = SystemColors.HighlightText;
				} else {
					e.Graphics.FillRectangle(new SolidBrush(e.SubItem.BackColor), e.Bounds);
					color = e.SubItem.ForeColor;
				}
				// if (CustomGridLines)
				// {
				// 	Rectangle rect = new Rectangle(e.Bounds.Left, e.Bounds.Top, e.Bounds.Width, e.Bounds.Height);
				// 	if (ActGlobals.oFormActMain.opColorUserInterface.cbInvertLuminosity.Checked)
				// 		e.Graphics.DrawRectangle(new Pen(new SolidBrush(Color.Gray)), rect);
				// 	else
				// 		e.Graphics.DrawRectangle(new Pen(new SolidBrush(Color.LightGray)), rect);
				// }
				var stringAlignment = e.Header.TextAlign switch {
					HorizontalAlignment.Left => StringAlignment.Near,
					HorizontalAlignment.Right => StringAlignment.Far,
					_ => StringAlignment.Center
				};
				var stringFormat = new StringFormat();
				stringFormat.Alignment = stringAlignment;
				stringFormat.Trimming = StringTrimming.EllipsisCharacter;
				stringFormat.LineAlignment = StringAlignment.Center;
				var rectangle = e.ColumnIndex != 0 || stringAlignment != 0 ? e.Bounds : new Rectangle(e.Bounds.X + 5, e.Bounds.Y, e.Bounds.Width - 5, e.Bounds.Height);
				stringFormat.FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.LineLimit;
				e.Graphics.DrawString(e.SubItem.Text, e.SubItem.Font, new SolidBrush(color), rectangle, stringFormat);
			}
			e.DrawDefault = false;
		} catch (Exception ex) {
			e.DrawDefault = true;
			// ActGlobals.oFormActMain.WriteExceptionLog(ex, "ListViewNoFlicker_DrawSubItem");
		}
	}

	private static void ListViewNoFlicker_DrawColumnHeader(object? _, DrawListViewColumnHeaderEventArgs e) {
		try {
			if (e.Bounds.Width > 0) {
				var rect = e.Bounds with {
					X = e.Bounds.Left,
					Y = e.Bounds.Top
				};
				var flag = (e.State & ListViewItemStates.Selected) == ListViewItemStates.Selected;
				// if (ActGlobals.oFormActMain.opColorUserInterface.cbInvertLuminosity.Checked)
				// {
				// 	if (flag)
				// 		e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(63, 63, 75)), rect);
				// 	else
				// 		e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(31, 31, 31)), rect);
				// 	e.Graphics.DrawRectangle(new Pen(new SolidBrush(Color.Gray)), rect);
				// }
				// else
				{
					e.Graphics.FillRectangle(flag ? new SolidBrush(Color.FromArgb(238, 243, 255)) : new SolidBrush(Color.FromArgb(248, 250, 255)), rect);
					e.Graphics.DrawRectangle(new Pen(new SolidBrush(Color.LightGray)), rect);
				}
				var stringAlignment = e.Header.TextAlign switch {
					HorizontalAlignment.Left => StringAlignment.Near,
					HorizontalAlignment.Right => StringAlignment.Far,
					_ => StringAlignment.Center
				};
				var stringFormat = new StringFormat();
				stringFormat.Alignment = stringAlignment;
				stringFormat.Trimming = StringTrimming.EllipsisCharacter;
				stringFormat.LineAlignment = StringAlignment.Center;
				var rectangle = e.ColumnIndex != 0 || stringAlignment != 0 ? e.Bounds : new Rectangle(e.Bounds.X + 5, e.Bounds.Y, e.Bounds.Width - 5, e.Bounds.Height);
				stringFormat.FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.LineLimit;
				// if (ActGlobals.oFormActMain.opColorUserInterface.cbInvertLuminosity.Checked)
				// 	e.Graphics.DrawString(e.Header.Text, e.Font, new SolidBrush(Color.FromArgb(153, 153, 153)), rectangle, stringFormat);
				// else
				e.Graphics.DrawString(e.Header.Text, e.Font, new SolidBrush(e.ForeColor), rectangle, stringFormat);
			}
			e.DrawDefault = false;
		} catch (Exception ex) {
			e.DrawDefault = true;
			// ActGlobals.oFormActMain.WriteExceptionLog(ex, "ListViewNoFlicker_DrawColumnHeader");
		}
	}

	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public new void BeginUpdate() {
		base.BeginUpdate();
		updating = true;
		updateCount++;
	}

	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	public new void EndUpdate() {
		updating = false;
		base.EndUpdate();
		updateCount--;
	}

	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public void FlushUpdate() {
		while (updateCount > 0) EndUpdate();
	}

	protected override void WndProc(ref Message messg) {
		try {
			if (updating && antiFlicker && messg.Msg is 20 or 15)
				messg.Msg = 0;
			base.WndProc(ref messg);
		} catch (Exception ex) {
			// ActGlobals.oFormActMain.WriteExceptionLog(ex, messg.ToString());
		}
	}

	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public void SetExStyles() {
		CustomGridLines = GridLines;
		GridLines = false;
		antiFlicker = true;
		// styles = (LVS_EX)SendMessage(base.Handle, 4151, 0, 0);
		// styles |= (LVS_EX)98304;
		// SendMessage(base.Handle, 4150, 0, (int)styles);
	}
}