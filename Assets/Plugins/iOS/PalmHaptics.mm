// Vibrations courtes de l'iPhone (Taptic Engine) pour PalmStreet : 0 légère, 1 moyenne, 2 forte.
#import <UIKit/UIKit.h>

extern "C" void PalmHaptic(int style)
{
    UIImpactFeedbackStyle s = style == 0 ? UIImpactFeedbackStyleLight : style == 1 ? UIImpactFeedbackStyleMedium : UIImpactFeedbackStyleHeavy;
    UIImpactFeedbackGenerator *g = [[UIImpactFeedbackGenerator alloc] initWithStyle:s];
    [g impactOccurred];
}
