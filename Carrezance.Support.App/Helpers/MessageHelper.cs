using System.Windows;
using Carrezance.Support.App.Models;

namespace Carrezance.Support.App.Helpers;

public static class MessageHelper
{
    public static bool AskRestartAsAdministrator()
    {
        return MessageBox.Show(
            "Cette action nécessite les droits administrateur." +
            Environment.NewLine +
            Environment.NewLine +
            "Voulez-vous relancer Carrezance Support en tant qu'administrateur ?",
            "Droits administrateur requis",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }

    public static bool Confirm(string message)
    {
        return MessageBox.Show(
            message,
            "Confirmation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question) == MessageBoxResult.Yes;
    }

    public static void ShowResult(ActionResult result)
    {
        MessageBox.Show(
            result.Message,
            "Carrezance Support",
            MessageBoxButton.OK,
            result.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    public static void ShowExportResult(ActionResult result)
    {
        var message = result.Success && !string.IsNullOrWhiteSpace(result.Details)
            ? $"{result.Message}{Environment.NewLine}{result.Details}"
            : result.Message;

        MessageBox.Show(
            message,
            "Carrezance Support",
            MessageBoxButton.OK,
            result.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    public static void ShowError(string message)
    {
        MessageBox.Show(message, "Carrezance Support", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

}
