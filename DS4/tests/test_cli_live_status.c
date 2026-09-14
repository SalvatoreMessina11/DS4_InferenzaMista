/* Exercise the actual CLI status timer without a model or network. */
#define main ds4_cli_unused_main
#include "../ds4_cli.c"
#undef main
#include <assert.h>

int main(void) {
    const struct timespec delay = {0, 600000000};
    cli_live_begin("Prefill", 64, cli_now_sec());
    cli_live_count_set(16);
    nanosleep(&delay, NULL);
    cli_live_count_set(8); /* Delayed callbacks must not move backwards. */
    if (cli_live_active) assert(cli_live_count == 16);
    nanosleep(&delay, NULL); /* Updates continue with no new completed block. */
    cli_live_begin("Generazione", 32, cli_now_sec());
    cli_live_count_set(3);
    nanosleep(&delay, NULL);
    cli_live_end();
    assert(!cli_live_active);
    return 0;
}
