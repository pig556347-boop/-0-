from pathlib import Path
from itertools import product
from tree_sitter import Language, Parser
import tree_sitter_c_sharp

root = Path(__file__).resolve().parents[1]
parser = Parser(Language(tree_sitter_c_sharp.language()))
count = 0
for path in (root / 'Assets').rglob('*.cs'):
    for unity6 in (False, True):
        for input_system, editor, webgl, release in product((False, True), repeat=4):
            defines = {'UNITY_6000_0_OR_NEWER': unity6,
                       'KICKBACK_RELEASE': release, 'ENABLE_INPUT_SYSTEM': input_system, 'UNITY_EDITOR': editor, 'UNITY_WEBGL': webgl}
            output, stack, active = [], [], True
            for line in path.read_text().splitlines():
                directive = line.strip()
                if directive.startswith('#if '):
                    condition = all((not defines[term[1:]]) if term.startswith('!') else defines[term] for term in directive[4:].split(' && '))
                    stack.append((active, condition))
                    active = active and condition
                elif directive == '#else':
                    parent, condition = stack[-1]
                    active = parent and not condition
                elif directive == '#endif':
                    active = stack.pop()[0]
                elif active:
                    output.append(line)
            assert not stack
            tree = parser.parse('\n'.join(output).encode())
            if tree.root_node.has_error:
                raise RuntimeError(f'C# syntax failure: {path}')
            count += 1
print(f'C# syntax: {count} file/configuration combinations passed')
