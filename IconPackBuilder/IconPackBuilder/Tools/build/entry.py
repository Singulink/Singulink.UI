"""PyInstaller entry point for the bundled pyftsubset binary."""

import sys

from fontTools.subset import main

if __name__ == "__main__":
    sys.exit(main())
