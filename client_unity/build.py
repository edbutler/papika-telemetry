
import os, sys
import subprocess
import argparse

localdir = os.path.abspath(os.path.dirname(__file__))

def main(args):
    def check_output(args, cwd):
        '''Wrapper for subprocess.check_output that converts result to unicode'''
        result = subprocess.check_output(args, cwd=cwd)
        return result.decode('utf-8')

    revid = check_output(['git', 'rev-parse', 'HEAD'], localdir).strip()
    status = check_output(['git', 'status', '--porcelain'], localdir).strip()

    if len(status) > 0:
        sys.stderr.write("Warning: Library is not clean, so embedded revision id is invalid!\nPlease commit all changes to suprress this error.\n")
        revid += "+"

    source_dir = os.path.join(localdir,'Assets/Code/')
    sources = ['Datatypes.cs', 'MicroJSON.cs', 'PapikaClient.cs', 'UnityBackend.cs']

    for fname in sources:
        with open(os.path.join(source_dir,fname)) as f:
            source = f.read()
        # super hacky string replacement to embed the revision id
        output = source.replace('UNKNOWN_REVISION_ID', revid)
        with open (os.path.join(args.outdir,fname),'wb') as d:
            d.write(output.encode('utf-8'))

parser = argparse.ArgumentParser(description="Build the papika client library. This embeds the revision id.")
parser.add_argument('outdir', help='Destination directory.')
main(parser.parse_args())

