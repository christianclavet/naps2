using System.Windows.Input;
using Eto.Drawing;
using Eto.Forms;
using NAPS2.Scan;

namespace NAPS2.EtoForms.Layout;

public static class C
{
    /// <summary>
    /// Creates a label with wrapping disabled. For WinForms support, all labels must not wrap.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static Label NoWrap(string text) =>
        new Label { Text = text, Wrap = WrapMode.None };

    /// <summary>
    /// Creates a link button with the specified text and action.
    /// If the action is not specified, it will assume the text is a URL to be opened.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="onClick"></param>
    /// <returns></returns>
    public static LinkButton Link(string text, Action? onClick = null)
    {
        onClick ??= () => Process.Start(text);
        return new LinkButton
        {
            Text = text,
            Command = new ActionCommand(onClick)
        };
    }

    /// <summary>
    /// Creates a button with the specified text and action.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="onClick"></param>
    /// <returns></returns>
    public static Button Button(string text, Action onClick) =>
        Button(text, new ActionCommand(onClick));

    /// <summary>
    /// Creates a button with the specified text and command.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="command"></param>
    /// <returns></returns>
    public static Button Button(string text, ICommand command) =>
        new Button
        {
            Text = text,
            Command = command
        };

    public static Button Button(Command command) =>
        Button(command.MenuText, command);

    public static Button Button(Command command, ButtonImagePosition imagePosition)
    {
        var button = Button(command);
        button.Image = command.Image;
        button.ImagePosition = imagePosition;
        EtoPlatform.Current.ConfigureImageButton(button);
        return button;
    }

    public static Button Button(Command command, Image image, ButtonImagePosition imagePosition = default)
    {
        var button = Button(command);
        button.Image = image;
        button.ImagePosition = imagePosition;
        EtoPlatform.Current.ConfigureImageButton(button);
        return button;
    }

    /// <summary>
    /// Creates a null placeholder for Eto layouts.
    /// 
    /// For example, it can be added to a row or column to absorb scaling.
    /// If it isn't at the end of the row/column, it must be annotated with .XScale() or .YScale(). 
    /// </summary>
    /// <returns></returns>
    public static ControlWithLayoutAttributes ZeroSpace() =>
        new ControlWithLayoutAttributes(null);

    /// <summary>
    /// Creates an label of default height to be used as a visual paragraph separator.
    /// </summary>
    /// <returns></returns>
    public static LayoutElement TextSpace() => NoWrap(" ");

    /// <summary>
    /// Creates a hacky image button that supports accessible interaction.
    /// 
    /// It works by overlaying an image on top a button.
    /// If the image has transparency an offset may need to be specified to keep the button hidden.
    /// If the text is too large relative to the button it will be impossible to hide fully.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="text"></param>
    /// <param name="onClick"></param>
    /// <param name="xOffset"></param>
    /// <param name="yOffset"></param>
    /// <returns></returns>
    public static Control AccessibleImageButton(Image image, string text, Action onClick, int xOffset = 0,
        int yOffset = 0)
    {
        var imageView = new ImageView { Image = image, Cursor = Eto.Forms.Cursors.Pointer };
        imageView.MouseDown += (_, _) => onClick();
        var button = new Button
        {
            Text = text,
            Width = 0,
            Height = 0,
            Command = new ActionCommand(onClick)
        };
        var pix = new PixelLayout();
        pix.Add(button, xOffset, yOffset);
        pix.Add(imageView, 0, 0);
        return pix;
    }

    public static Label Label(string text)
    {
        return new Label
        {
            Text = text
        };
    }

    public static DropDown EnumDropDown<T>() where T : Enum
    {
        return EnumDropDown<T>(x => x.Description());
    }

    public static DropDown EnumDropDown<T>(Func<T, string> format) where T : Enum
    {
        var combo = new DropDown();
        foreach (var item in Enum.GetValues(typeof(T)))
        {
            combo.Items.Add(new ListItem
            {
                Key = item.ToString(),
                Text = format((T) item)
            });
        }
        return combo;
    }
}