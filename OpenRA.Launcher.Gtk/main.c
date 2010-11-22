#include <gtk/gtk.h>
#include <webkit/webkit.h>

GtkWindow * window;
WebKitWebView * browser;

gboolean window_delete(GtkWidget * widget, GdkEvent * event, gpointer user_data)
{
  gtk_main_quit();
  return TRUE;
}

int get_file_uri(char const * filepath, char * uri)
{
  FILE * stdout;
  char buffer[1024];

  sprintf(buffer, "readlink -f %s", filepath);

  stdout = popen(buffer, "r");

  if (!stdout)
  {
    printf("Could not find absolute path for %s", filepath);
    return FALSE;
  }

  fgets(buffer, sizeof(buffer), stdout);

  pclose(stdout);

  sprintf(uri, "file://%s", buffer);

  return TRUE;
}

int main(int argc, char ** argv)
{
  char uri[1024];
  gtk_init(&argc, &argv);

  window = GTK_WINDOW(gtk_window_new(GTK_WINDOW_TOPLEVEL));
  gtk_window_set_title(window, "OpenRA Launcher");
  gtk_window_set_default_size(window, 800, 600);

  browser = WEBKIT_WEB_VIEW(webkit_web_view_new());

  gtk_container_add(GTK_CONTAINER(window), GTK_WIDGET(browser));

  get_file_uri("../mods/cnc/mod.html", uri);

  webkit_web_view_load_uri(browser, uri);

  gtk_widget_show_all(GTK_WIDGET(window));
  g_signal_connect(window, "delete-event", G_CALLBACK(window_delete), 0);

  gtk_main();

  return 0;
}
